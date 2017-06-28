module EasySyncClient.CmisClient

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open DotCMIS.Enums
open System.Collections.Generic
open EasySyncClient.Models
open EasySyncClient.ClientModels
open EasySyncClient.Utility
open EasySyncClient.DB
open System.Threading
open System.Text
open System.IO
open System
open System.Web
open System.Linq

type UpdateStatus =
    | Success
    | Failed of string
    | Skip

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

type CmisClient (settings, folder) as this = 

    let remoteRoot = folder.RemotePath
    let localRoot = folder.LocalPath

    let session = createSession(settings)

    let createRemoteRelative (fullPath: string) = 
        fullPath.Replace(remoteRoot, "").TrimStart('/') 

    let createLocalRelative (fullPath: string) = 
        fullPath.Replace(localRoot, "").TrimStart('/').TrimStart('\\')

    let createLocalPath relative =
        Path.Combine(folder.LocalPath, relative)

    let createRemotePath (relative: string) = 
        let trim = relative.TrimStart('/').TrimStart('\\').Replace('\\', '/')
        remoteRoot + "/" + trim

    let rec syncLocalChild (folder: DirectoryInfo) = 
        let dirs = folder.GetDirectories()
        let files = folder.GetFiles()
        for dir in dirs do
            let fullPath = dir.FullName
            let relative = createLocalRelative fullPath
            let remotePath = createRemotePath relative
            let remoteFolder = this.GetFolder remotePath
            match remoteFolder with
            | Some folder ->
                syncLocalChild dir
            | None ->
                let db = DbManager.queryFolder remoteRoot relative
                match db with
                | Some folder ->
                    log "try to delete local folder | %s" dir.FullName
                    dir.Delete(true)
                    DbManager.deleteFolder folder.Id |> ignore
                | None ->
                    this.CreateFolders remotePath |> ignore
                    let newFolder = { QFolder.Id = 0; RemoteRoot = remoteRoot; RelativePath = relative }
                    DbManager.updateFolder newFolder |> ignore
                    syncLocalChild dir

        for file in files.Where(fun x -> x.Name.StartsWith(".") |> not) do
            let fullPath = file.FullName
            let relative = createLocalRelative fullPath
            let remotePath = createRemotePath relative
            let remote : IDocument option  = this.GetDocument remotePath
            match remote with
            | None ->
                let db = DbManager.queryFile remoteRoot relative
                match db with
                | None ->
                    this.UpdateDocument remotePath fullPath |> ignore
                    let newFile = { QFile.Id = 0; RemoteRoot = remoteRoot; RelativePath = relative; Md5 = "" }
                    DbManager.updateFile newFile |> ignore
                | Some file ->
                    File.Delete fullPath
                    DbManager.deleteFile file.Id |> ignore
            | Some file ->
                let last = file.LastModificationDate.Value
                if File.GetLastWriteTime(fullPath) < last then
                    this.UpdateDocument  remotePath fullPath |> ignore

    let rec syncRemoteChild (folder: IFolder) = 
        let childs = folder.GetChildren()
        for child in childs do
            if child :? DotCMIS.Client.Impl.Folder then
                log "process folder => %A" folder.Path
                let remoteFolder = child :?> IFolder    
                let fullPath = remoteFolder.Path
                let relative = createRemoteRelative fullPath
                let localPath = createLocalPath relative
                if Directory.Exists localPath then
                    syncRemoteChild remoteFolder
                else 
                    let db = DbManager.queryFolder remoteRoot relative 
                    match db with
                    | Some folder ->
                        log "try to remote remote folder | %s" fullPath    
                        this.DeleteFolder fullPath          |> ignore
                        DbManager.deleteFolder folder.Id    |> ignore
                    | None -> 
                        let newDb = { Id = 0; RemoteRoot = remoteRoot; RelativePath = relative }
                        DbManager.updateFolder newDb            |> ignore
                        Directory.CreateDirectory localPath     |> ignore
                
            else if child :? DotCMIS.Client.Impl.Document then
                let remoteFile = child :?> IDocument
                let fullPath = String.Join("/", remoteFile.Paths)

                log "process file => %A" fullPath

                let relative = createRemoteRelative fullPath
                let localPath = createLocalPath relative

                if File.Exists localPath then
                    if child.LastModificationDate.Value > File.GetLastWriteTime localPath then
                        this.DowloadDocument fullPath localPath |> ignore
                    else
                        this.UpdateDocument fullPath localPath  |> ignore
                else
                    let db = DbManager.queryFile remoteRoot relative
                    match db with
                    | Some file ->
                        this.DeleteDocument fullPath    |> ignore
                        DbManager.deleteFile file.Id    |> ignore
                    | None ->
                        let newDb = { QFile.Id = 0; RemoteRoot = remoteRoot; RelativePath = relative; Md5 = "" }
                        DbManager.updateFile newDb              |> ignore
                        this.DowloadDocument fullPath localPath |> ignore

    member private this.IsExist path count = 
        try 
            let rs = session.GetObjectByPath(path)
            Some rs
        with ex -> 
            if count < 3 then
                Thread.Sleep 300
                this.IsExist path (count + 1)
            else
                None

    member this.MoveFile originalPath newPath  = 
        let doc = session.GetObjectByPath(originalPath)
        ()

    member this.DeleteFolder targetPath = 
        let folder = session.GetObjectByPath targetPath :?> IFolder
        folder.DeleteTree(true, UnfileObject.Delete |> Nullable, true) |> ignore
        Success

    member this.CreateFolders (targetPath: string) = 
        let createDir rootPath name = 
            let nextPath = 
                if rootPath = "/" then 
                    rootPath + name
                else
                    rootPath + "/" + name
            log "next path => %A" nextPath

            match this.IsExist nextPath 0 with
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

    member private this.GetFolder targetPath = 
        try 
            let folder = session.GetObjectByPath targetPath :?> IFolder
            Some folder
        with ex ->
            None

    member private this.GetDocument targetPath = 
        try 
            let doc = session.GetObjectByPath targetPath :?> IDocument
            doc.GetObjectOfLatestVersion(false) |> Some
        with ex ->
            None

    member this.Rename originalPath newName = 
        let doc = this.GetDocument originalPath
        match doc with
        | None -> 
            Skip
        | Some doc ->
            doc.Rename(newName) |> ignore
            Success

    member this.DeleteDocument targetPath = 
        let doc = this.GetDocument targetPath
        match doc with
        | None ->
            Failed "File not exist"
        | Some d ->
            d.Delete(true)
            Success 

    member this.Touch targetPath dateTime = 
        let document = this.GetDocument targetPath
        match document with 
        | Some document ->
            let properties = 
                dict [
                    "cmis:lastModificationDate", dateTime
                ]
            document.UpdateProperties properties |> ignore
        | None -> ()

    member this.UpdateDocument targetPath localPath = 
        let date = File.GetLastWriteTime localPath
        let doc = this.GetDocument targetPath
        let result = 
            match doc with
            | None -> 
                this.CreateDocument targetPath localPath
            | Some document -> 
                let mimetype = MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(localPath))
                use stream = new FileStream(localPath, FileMode.Open, FileAccess.Read)
                let length = stream.Length
                let contentStream = session.ObjectFactory.CreateContentStream(document.Name, int64 length, mimetype, stream);
                document.SetContentStream(contentStream, true) |> ignore
                Success
        this.Touch targetPath date
        (result)

    member this.DowloadDocument remotePath downloadPath = 
        let localInfo = FileInfo(downloadPath)
        let remote = session.GetObjectByPath(remotePath) :?> IDocument
        this.StartDownload remote downloadPath

    member private this.StartDownload (remote : IDocument) downloadPath = 
        let last = remote.LastModificationDate.Value
        do
            let remoteStream = remote.GetContentStream()
            use fileStream = new FileStream(downloadPath, FileMode.Create)
            let stream = remoteStream.Stream
            stream.CopyTo(fileStream)
        File.SetLastWriteTime(downloadPath, last)

    member private this.GetMimetype localPath = 
        MimeTypes.MimeTypeMap.GetMimeType (Path.GetExtension localPath)

    member private this.CreateDocument targetPath localPath = 
        let (path, name) = extractFullRemotePath (FullRemotePath targetPath)
        this.CreateFolders path |> ignore

        let mimetype = this.GetMimetype localPath
        let stream = new FileStream(localPath, FileMode.Open)
        let length = stream.Length

        let contentStream = session.ObjectFactory.CreateContentStream(name, int64 length, mimetype, stream);
        let properties = new Dictionary<string, Object>()
        properties.[PropertyIds.Name] <- name
        properties.[PropertyIds.ObjectTypeId] <- "cmis:document"

        try 
            let parent = session.GetObjectByPath (path) :?> IFolder
            let newDoc = parent.CreateDocument(properties, contentStream, VersioningState.Minor |> Nullable)
            let date = newDoc.LastModificationDate.Value;
            File.SetLastWriteTime(localPath, date)
            Success
        with ex -> 
            let exist = this.IsExist targetPath 0
            match exist with
            | Some obj -> 
                Success 
            | None ->
                Failed ex.Message

    member this.DownSync() = 
        log "downsync root = %A" remoteRoot
        let folder = session.GetObjectByPath(remoteRoot) :?> IFolder
        syncRemoteChild folder

    member this.UpSync() =
        log "upsync root = %A" localRoot
        let folder = DirectoryInfo(localRoot)
        syncLocalChild folder

