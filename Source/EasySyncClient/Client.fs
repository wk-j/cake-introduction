module EasySyncClient.Client

open WebDAVClient
open System
open System.Threading
open Newtonsoft.Json
open System.IO
open EasySyncClient.Models
open EasySyncClient
open System.Linq
open System.Net
open WebDAVClient
open System.Threading.Tasks
open EasySyncClient.FileWatcher
open NLog

let splitWith (sep: string) (str: string) = str.Split([| sep |], StringSplitOptions.None)
let cleanPath (str: string) = str.TrimEnd('/').Replace("//", "/").Replace("\\", "/")
let toSections (data: string array) = [ for i in 0..data.Length do yield String.concat "/"  <| data.Take(i) ]
let replace (a:string) b (c: string)= c.Replace(a, b)

type RemoteRelativePath = RemoteRelativePath of string
type RemoteRelativeNoName = RemoteRelativeNoName of string
type LocalRelativePath = LocalRelativePath of string

type FullRemotePath = FullRemotePath of string
type FullRemotePathNoName = FullRemotePathNoName of string

type FullLocalPath = FullLocalPath of string

type RemoteSection = RemoteSection of string array
type RemoteRoot = RemoteRoot of string
type LocalRoot = LocalRoot of string

let createRelativeRemotePath (LocalRoot localRoot) (FullLocalPath localPath) = 
    let relative = replace localRoot "" localPath 
    let path = Path.GetDirectoryName relative
    let name = Path.GetFileName relative 
    relative |> RemoteRelativePath

let createRemoteSection (RemoteRelativeNoName path) =
    let path = path |> cleanPath
    splitWith "/" path |> toSections |> List.map RemoteRelativePath

let createFullRemotePath (RemoteRoot root) (RemoteRelativePath path)  = 
    root + "/" + path |> cleanPath  |> FullRemotePath

let createFullRemotePathNoName (RemoteRoot root) (RemoteRelativeNoName path) = 
    root + "/" + path |> cleanPath  |> FullRemotePathNoName

let extractFullRemotePath (FullRemotePath path) = 
    let last = path |> splitWith "/" |> Array.last
    let path = Path.GetDirectoryName path
    (path, last)

type Action  = 
    | CreateDir of RemoteRoot * RemoteRelativePath
    | CreateFile of RemoteRelativePath * LocalRelativePath

type MoveInfo = {
    OldPath : string
    NewPath : string
}

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

    member this.MoveFile (RemoteRoot remoteRoot) (LocalRoot localRoot) info = 
        async {
            let fullRemotePath (FullRemotePath remote )  = remote
            let extractPath localPath = 
                let path = createRelativeRemotePath (LocalRoot localRoot) (FullLocalPath localPath) 
                let remote = RemoteRoot remoteRoot
                let targetPath = createFullRemotePath remote path
                fullRemotePath targetPath

            let newFileName = Path.GetFileName(info.NewPath)
            let originalPath = extractPath info.OldPath
            let newPath = extractPath info.NewPath 

            printfn "move %s to %s" originalPath newPath

            let! rs = client.MoveFile(originalPath, newPath) |> Async.AwaitTask
            return rs
        } |> Async.RunSynchronously

    member this.UploadFile (RemoteRoot remoteRoot) (LocalRoot localRoot) (FullLocalPath localPath) = 
        if localPath.Contains localRoot then
            async {
                let path = createRelativeRemotePath (LocalRoot localRoot) (FullLocalPath localPath)
                let remote = RemoteRoot remoteRoot

                let getPath (RemoteRelativePath path) = Path.GetDirectoryName path |> RemoteRelativeNoName 

                this.TryCreateDirectories (RemoteRoot remoteRoot) (getPath path) |> ignore
                let targetPath = createFullRemotePath remote path

                let path, name = extractFullRemotePath targetPath
                
                printfn "try to upload file %s => %s" path name

                let! item =  client.Upload(path, File.OpenRead(localPath), name) |> Async.AwaitTask
                return item
            }
            |> Async.RunSynchronously
            |> ignore
            true
        else
            false

type ChangeManager(endPoint, config) as this = 

    let changeMonitor = new ChangeWatcher()
    let settings = { Path = config.LocalPath; Pattern = "*.*" }
    let client = AlfrescoClient endPoint

    do 
        changeMonitor.Watch settings this.ProcessChange

    member this.ProcessChange(change) = 
        let fullPath = change.FullPath
        let fullLocalPath = FullLocalPath fullPath
        let localRoot = LocalRoot config.LocalPath
        let remoteRoot = RemoteRoot config.RemotePath
        match change.FileStatus with
        | Created -> 
            client.UploadFile remoteRoot localRoot fullLocalPath |> ignore
        | Deleted -> ()
        | Renamed (old, n) -> 
            let info = 
              { OldPath = old
                NewPath = n }
            client.MoveFile remoteRoot localRoot info  |> ignore
        | Changed -> 
            client.UploadFile remoteRoot localRoot fullLocalPath |> ignore

type FolderManager(endPoint, config) as this =

    let timer = new System.Timers.Timer(10000.0)    
    let client = AlfrescoClient(endPoint)

    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member private this.Upload(file) = 
        let full, rel = file
        let fullPath = config.RemotePath
        let sections = Path.GetDirectoryName rel  |> splitWith "/"  |> toSections
        ()

    member this.ManualSync() =
        let local = config.LocalPath
        let startUpload (fileInfo: FileInfo) = 
            printfn "start upload %s" fileInfo.FullName
            let localRoot = LocalRoot config.LocalPath
            let remoteRoot = RemoteRoot config.RemotePath
            let full = FullLocalPath fileInfo.FullName
            client.UploadFile remoteRoot localRoot full

        let findModifyFiles() = 
            printfn "file modified files %s" config.LocalPath
            let last = SettingsManager.getTouchDate (config)
            Directory.EnumerateFiles(local, "*.txt", SearchOption.AllDirectories)
                |> Seq.map FileInfo
                |> Seq.filter(fun x -> x.LastWriteTime >= last)

        let files = findModifyFiles() 
        let results = files |> Seq.map startUpload |> Seq.toList
        (results)

    member private this.Process(args) =
        printfn "process ..."
        let pause = timer.Stop
        let resume = timer.Start
        let touch() = SettingsManager.touch config
        let sync = this.ManualSync

        let ps = pause >> sync >> ignore >> touch >> resume 
        ps()

    member this.StartTimer = timer.Start
    member this.StopTimer = timer.Stop

    interface IDisposable with
        member this.Dispose() = timer.Dispose()

type SyncManger() = 

    member this.CreateFolderManager(endPoint, folder) = 
        new FolderManager(endPoint, folder)

    member this.StartSync() = 
        let config = SettingsManager.localConfig()
        let endPoint = config.EndPoint
        let folders = config.Folders

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.StartTimer())