
module EasySyncClient.CmisClient

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open System.Collections.Generic
open EasySyncClient.Models
open EasySyncClient.ClientModels
open EasySyncClient.Utility
open System.Threading
open System.IO
open System

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

    member this.CreateFolders  rootPath relativePath = 
        let getRawPath (FullRemotePath path) = path
        let getRootPath(RemoteRoot root) = root;

        //let createDir (folder: IFolder) name = 
        let createDir rootPath name = 
            let nextPath = rootPath + "/" + name
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

        let rootPath = getRootPath rootPath
        let root = session.GetObjectByPath(rootPath) :?> IFolder

        let sections = splitWith "/" relativePath
        sections |> Array.fold (createDir) root.Path

    member this.StartSync() = 
        let folder = session.GetObjectByPath(remoteRoot) :?> IFolder
        syncChild folder