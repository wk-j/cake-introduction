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
open EasySyncClient.DB
open NLog

let private createFile file newPath action =
    let info = FileInfo(file)
    let model = 
        { QFile.Status = Status.Initialize
          CreationTime = info.CreationTime
          LastWriteTime = info.LastWriteTime
          LastAccessTime = info.LastAccessTime
          OriginalPath = info.FullName
          NewPath = newPath
          Id = 0
          FileAction = action }
    DbManager.updateFile model

type ChangeManager(config) = 

    let changeMonitor = new ChangeWatcher()
    let settings = { Path = config.LocalPath; Pattern = "*.*" }
    let mutable running = false

    member this.StartWatch() =
        if not running then
            changeMonitor.Watch settings this.ProcessChange
            running <- true

    member private this.ProcessChange(change) = 
        let fullPath = change.FullPath

        let result = 
            match change.FileStatus with
            | Created -> 
                createFile (fullPath) "" FileAction.Created
            | Deleted -> 
                createFile (fullPath) "" FileAction.Deleted
            | Renamed (old, n) -> 
                createFile (fullPath) n FileAction.Renamed
            | Changed -> 
                createFile (fullPath) "" FileAction.Changed
        result |> ignore

type FolderManager(config) as this =

    let timer = new System.Timers.Timer(10000.0)    

    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member this.ManualSync() =
        let local = config.LocalPath
        let startUpload (fileInfo: FileInfo) = 
            printfn "start upload %s" fileInfo.FullName
            createFile (fileInfo.FullName) "" FileAction.Changed

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
        new FolderManager(folder)

    member this.StartSync() = 
        let config = SettingsManager.globalConfig()
        let endPoint = config.EndPoint
        let folders = config.Folders

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.StartTimer())