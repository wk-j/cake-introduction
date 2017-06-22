module EasySyncClient.Managers

open WebDAVClient
open System
open System.Threading
open System.IO
open EasySyncClient.Models
open EasySyncClient
open System.Linq
open System.Net
open System.Threading.Tasks
open EasySyncClient.FileWatcher
open EasySyncClient.UploadManager
open EasySyncClient.ClientModels
open EasySyncClient.DB
open EasySyncClient.Utility
open NLog

let private createFile (RemoteRoot remoteRoot) (LocalRoot localRoot) file newPath action =
    let info = FileInfo(file)
    let model = 
        { QFile.Status = Status.Initialize
          CreationTime = info.CreationTime
          LastWriteTime = info.LastWriteTime
          LastAccessTime = info.LastAccessTime
          OriginalPath = info.FullName
          RemoteRoot = remoteRoot
          LocalRoot = localRoot
          NewPath = newPath
          Id = 0
          FileAction = action }
    DbManager.updateFile model

type ChangeManager(config) = 
    let changeMonitor = new ChangeWatcher()
    let settings = { Path = config.LocalPath; Pattern = "*.txt" }
    let mutable running = false

    member this.StartWatch() =
        if not running then
            changeMonitor.Watch settings this.ProcessChange
            running <- true

    member private this.ProcessChange(change) = 
        let fullPath = change.FullPath
        let remoteRoot = RemoteRoot config.RemotePath
        let localRoot = LocalRoot config.LocalPath

        let createAction = createFile remoteRoot localRoot fullPath

        let result = 
            match change.FileStatus with
            | Created -> 
                createAction "" FileAction.Created
            | Deleted -> 
                createAction "" FileAction.Deleted
            | Renamed (old, n) -> 
                createAction  n FileAction.Renamed
            | Changed -> 
                createAction "" FileAction.Changed
        result |> ignore

type FolderManager(config) as this =
    let timer = new System.Timers.Timer(10000.0)    
    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member this.ManualSync() =
        let localPath = config.LocalPath
        let local = LocalRoot localPath 
        let remote = RemoteRoot config.RemotePath
        let startUpload (fileInfo: FileInfo) = 
            log "start upload %s" fileInfo.FullName
            createFile remote local (fileInfo.FullName) "" FileAction.Changed

        let findModifyFiles() = 
            log "file modified files %s" config.LocalPath
            let last = SettingsManager.getTouchDate (config)
            Directory.EnumerateFiles(localPath, "*.txt", SearchOption.AllDirectories)
                |> Seq.map FileInfo
                |> Seq.filter(fun x -> x.LastWriteTime >= last)

        let files = findModifyFiles() 
        let results = files |> Seq.map startUpload |> Seq.toList
        (results)

    member private this.Process(args) =
        let pause = timer.Stop
        let resume = timer.Start
        let touch() = SettingsManager.touch config
        let sync = this.ManualSync

        let ps = pause >> sync >> ignore >> touch // >> resume 
        ps()

    member this.StartTimer = timer.Start
    member this.StopTimer = timer.Stop

    interface IDisposable with
        member this.Dispose() = timer.Dispose()

type SyncManger() = 
    member this.CreateFolderManager(endPoint, folder) = 
        new FolderManager(folder)

    member this.StartSync() = 
        let config = SettingsManager.globalConfig()
        let endPoint = config.EndPoint
        let folders = config.Folders

        let folderManagers = 
            folders |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.StartTimer())

        let change = ChangeManager(config.Folders.[0])
        change.StartWatch()

        UploadManager.start config.EndPoint

