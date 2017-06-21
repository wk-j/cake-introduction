module EasySyncClient.AlfrescoClient

open NLog
open WebDAVClient
open EasySyncClient.ClientModels
open EasySyncClient.Models
open System.Net
open System.IO
open System.Threading.Tasks

type AlfrescoClient(endPoint) = 
    let logger = LogManager.GetCurrentClassLogger()

    let client = Client(NetworkCredential(UserName = endPoint.User, Password = endPoint.Password))
    do 
        client.Server <- endPoint.Url

    member this.ProcessAction = function
        | CreateDir (remoteRoot, remotePath) ->
            this.TryCreateDirectory remoteRoot remotePath
        | _ -> false

    member this.TryCreateDirectory remoteRoot remotePath = 
        let full = createFullRemotePath remoteRoot remotePath |> fun (FullRemotePath path) -> path

        async {
            do! Task.Delay(1000) |> Async.AwaitTask
            try 
                logger.Info("Try to create directory | {0}", full)
                let! items = client.GetFolder full |> Async.AwaitTask
                return true
            with ex ->
                try 
                    let path, last = extractFullRemotePath (FullRemotePath full)
                    logger.Info("Create {0} => {1}", path, last)
                    client.CreateDir(path, last) |> Async.AwaitTask |> ignore
                    return true
                with ex ->
                    return false
        } |> Async.RunSynchronously

    member this.TryCreateDirectories remoteRoot remotePath  =
        async {
            let full = createFullRemotePathNoName remoteRoot remotePath |> fun (FullRemotePathNoName path) -> path
            printfn "try to create directory %s" full
            try 
                let! item = client.GetFolder full |> Async.AwaitTask
                logger.Info("{0} is exists", full)
                return true
            with ex ->
                let sections = createRemoteSection remotePath 
                let rootMap = this.TryCreateDirectory remoteRoot
                sections |> List.map rootMap |> ignore
                return true
        } |> Async.RunSynchronously

    member this.DeleteFile remoteRoot localRoot fullLocalPath  = 
        async {
            let fullRemotePath (FullRemotePath remote) = remote
            let remotePath = createRelativeRemotePath localRoot fullLocalPath
            let targetPath = createFullRemotePath remoteRoot remotePath
            let str = fullRemotePath targetPath
            client.DeleteFile(str) |> Async.AwaitTask |> ignore
            return true
        } |> Async.RunSynchronously

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

            printfn "move %s to %s" originalPath newPath

            let! rs = client.MoveFile(originalPath, newPath) |> Async.AwaitTask
            return rs
        } |> Async.RunSynchronously

    member this.UploadFile remoteRoot localRoot localPath = 
        let  getLocalPath (FullLocalPath path) = path
        async {
            let path = createRelativeRemotePath localRoot localPath
            let getPath (RemoteRelativePath path) = Path.GetDirectoryName path |> RemoteRelativeNoName 

            this.TryCreateDirectories remoteRoot (getPath path) |> ignore
            let targetPath = createFullRemotePath remoteRoot path
            let path, name = extractFullRemotePath targetPath
            
            printfn "try to upload file %s => %s" path name

            let! item =  client.Upload(path, File.OpenRead(getLocalPath localPath), name) |> Async.AwaitTask
            return item
        } |> Async.RunSynchronously
