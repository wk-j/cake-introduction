module EasySyncClient.AlfrescoClient

open NLog
open WebDAVClient
open EasySyncClient.ClientModels
open EasySyncClient.Models
open System.Net
open System.IO
open System.Threading.Tasks
open EasySyncClient.Utility

type UpdateResult = 
    | Success
    | Failed of string
    | Skip

type AlfrescoClient(endPoint) = 

    let client = Client(NetworkCredential(UserName = endPoint.User, Password = endPoint.Password))
    do 
        client.Server <- endPoint.Url

    (*
    member this.ProcessAction = function
        | CreateDir (remoteRoot, remotePath) ->
            this.TryCreateDirectory remoteRoot remotePath
        | _ -> false
    *)

    member this.TryCreateDirectory remoteRoot remotePath = 
        let full = createFullRemotePath remoteRoot remotePath |> fun (FullRemotePath path) -> path

        async {
            do! Task.Delay(1000) |> Async.AwaitTask
            try 
                log "try to create directory %s" full
                let! items = client.GetFolder full |> Async.AwaitTask
                return Success
            with ex ->
                try 
                    let path, last = extractFullRemotePath (FullRemotePath full)
                    log "create %s => %s" path last
                    client.CreateDir(path, last) |> Async.AwaitTask |> ignore
                    return Success
                with ex ->
                    return Failed (ex.Message)
        } 

    member this.TryCreateDirectories remoteRoot remotePath  =
        async {
            let full = createFullRemotePathNoName remoteRoot remotePath |> fun (FullRemotePathNoName path) -> path
            log "try to create directory %s" full
            try 
                let! item = client.GetFolder full |> Async.AwaitTask
                return Success
            with ex ->
                let sections = createRemoteSection remotePath 
                let rootMap = this.TryCreateDirectory remoteRoot
                sections |> List.map rootMap |> ignore
                return Failed (ex.Message)
        } 

    member this.CanUpdate (FullRemotePath remote) lastModified =
        async {
            let! file = client.GetFile(remote) |> Async.AwaitTask
            if file.LastModified.Value < lastModified then
                return Success
            else
                return Skip
        } 

    member this.DeleteFile remoteRoot localRoot fullLocalPath  = 
        async {
            let fullRemotePath (FullRemotePath remote) = remote
            let remotePath = createRelativeRemotePath localRoot fullLocalPath
            let targetPath = createFullRemotePath remoteRoot remotePath
            let str = fullRemotePath targetPath
            client.DeleteFile(str) |> Async.AwaitTask |> ignore
            return Success
        } 

    member this.MoveFile remoteRoot localRoot info = 
        async {
            let fullRemotePath (FullRemotePath remote )  = remote
            let extractPath localPath = 
                let path = createRelativeRemotePath localRoot (FullLocalPath localPath) 
                let targetPath = createFullRemotePath remoteRoot path
                fullRemotePath targetPath

            let newFileName = Path.GetFileName(info.NewPath)
            let originalPath = extractPath info.OldPath
            let newPath = extractPath info.NewPath 

            log "move %s to %s" originalPath newPath

            let! rs = client.MoveFile(originalPath, newPath) |> Async.AwaitTask
            return Success
        } 

    member this.UploadFile remoteRoot localRoot localPath = 
        let getLocalPath (FullLocalPath path) = path
        let relativePath = createRelativeRemotePath localRoot localPath
        let getPath (RemoteRelativePath path) = Path.GetDirectoryName path |> RemoteRelativeNoName 
        let date = File.GetLastWriteTime (getLocalPath localPath)

        async {
            try 
                let targetPath = createFullRemotePath remoteRoot relativePath
                let path, name = extractFullRemotePath targetPath
                let! canUpdate = this.CanUpdate targetPath date
                match canUpdate with
                | Success ->
                    log "try to upload file %s => %s" path name
                    let! dir = this.TryCreateDirectories remoteRoot (getPath relativePath)
                    let! item =  client.Upload(path, File.OpenRead(getLocalPath localPath), name) |> Async.AwaitTask
                    return Success
                | Failed ex ->
                    return Failed ex
                | Skip ->
                    return Skip
            with ex ->
                return Failed (ex.Message)
        } 
