
module EasySyncClient.CmisClient

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open DotCMIS.Enums
open System.Collections.Generic
open EasySyncClient.Models
open EasySyncClient.ClientModels
open EasySyncClient.Utility
open System.Threading
open System.Text
open System.IO
open System
open System.Web

type SyncObject =
    | File of FullPath * RelativePath
    | Folder of FullPath * RelativePath
and FullPath = FullPath of string
and RelativePath = RelativePath of string

let private createSession (settings) = 
    let url = settings.Url
    let user = settings.User
    let pass = settings.Password

    let parameters =  Dictionary<string, string>();
    parameters.[SessionParameter.BindingType] <- BindingType.AtomPub
    parameters.[SessionParameter.AtomPubUrl] <- sprintf "%s/api/-default-/public/cmis/versions/1.1/atom" url
    parameters.[SessionParameter.User] <- user
    parameters.[SessionParameter.Password] <- pass
    parameters.[SessionParameter.RepositoryId] <- "-default-"

    let factory = SessionFactory.NewInstance()
    factory.CreateSession(parameters)

type CmisClient (settings, folder) = 
    let meetObject = new Event<SyncObject>()
    let onMeetObject = meetObject.Publish
    let remoteRoot = folder.RemotePath
    let localRoot = folder.LocalPath

    let session = createSession(settings)

    let createRelative root (FullPath fullPath) = 
        fullPath.Replace(root, "").TrimStart('/') |> RelativePath

    let rec syncChild (folder: IFolder) = 
        let childs = folder.GetChildren()
        for c in childs do
            if c :? DotCMIS.Client.Impl.Folder then
                let f = c :?> IFolder    
                let path = f.Path
                let full =  FullPath path
                let relative = createRelative remoteRoot full 
                meetObject.Trigger <| Folder (full, relative)
                syncChild f
            else if c :? DotCMIS.Client.Impl.Document then
                let d = c :?> IDocument
                let path = String.Join("/", d.Paths)
                let full = FullPath path
                let relative = createRelative remoteRoot full 
                meetObject.Trigger <| File (full, relative)

    member this.OnMeetObject = onMeetObject

    //member this.CreateFolders ()

    member this.Exist path count = 
        try 
            let rs = session.GetObjectByPath(path) :?> IFolder
            Some rs
        with ex -> 
            if count < 3 then
                Thread.Sleep 300
                this.Exist path (count + 1)
            else
                None

    member this.CreateFolders (targetPath: string) = 
        let createDir rootPath name = 
            let nextPath = 
                if rootPath = "/" then 
                    rootPath + name
                else
                    rootPath + "/" + name
            log "next path => %A" nextPath

            match this.Exist nextPath 0 with
            | None ->
                log "get path => %A" rootPath
                let root = session.GetObjectByPath(rootPath) :?> IFolder
                let properties = new Dictionary<string, Object>();
                properties.[PropertyIds.Name] <- name
                properties.[PropertyIds.ObjectTypeId] <- "cmis:folder";

                log "create dir %A => %A" root.Path name
                try 
                    root.CreateFolder(properties) |> ignore
                with ex -> ()
                nextPath
            | Some folder ->
                nextPath

        let sections = splitWith "/" (targetPath.TrimStart('/'))
        sections |> Array.fold (createDir) "/"

    member this.GetDocument targetPath = 
        try 
            session.GetObjectByPath targetPath :?> IDocument |> Some
        with ex ->
            None

    member this.UpdateDocument targetPath localPath = 
        let doc = this.GetDocument targetPath
        match doc with
        | None -> 
            this.CreateDocument targetPath localPath |> ignore
        | Some document -> 
            let mimetype = "plain/text"
            let stream = new FileStream(localPath, FileMode.Open)
            let length = stream.Length
            let contentStream = session.ObjectFactory.CreateContentStream(document.Name, int64 length, mimetype, stream);
            document.SetContentStream(contentStream, true, true) |> ignore

    member this.CreateDocument targetPath localPath = 
        let (path, name) = extractFullRemotePath (FullRemotePath targetPath)
        this.CreateFolders path |> ignore

        let mimetype = "plain/text"
        let stream = new FileStream(localPath, FileMode.Open)
        let length = stream.Length

        let contentStream = session.ObjectFactory.CreateContentStream(name, int64 length, mimetype, stream);
        let properties = new Dictionary<string, Object>()
        properties.[PropertyIds.Name] <- name
        properties.[PropertyIds.ObjectTypeId] <- "cmis:document"

        let parent = session.GetObjectByPath (path) :?> IFolder

        log "create document %A => %A" parent.Path name 
        try 
            let newDoc = parent.CreateDocument(properties, contentStream, VersioningState.Minor |> Nullable)
            true
        with ex -> 
            false

    member this.StartSync() = 
        let folder = session.GetObjectByPath(remoteRoot) :?> IFolder
        syncChild folder