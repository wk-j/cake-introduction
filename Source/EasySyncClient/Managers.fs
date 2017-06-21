module EasySyncClient.Managers

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
open EasySyncClient.DB
open EasySyncClient.ClientModels
open EasySyncClient.AlfrescoClient
open NLog

type ChangeManager(endPoint, config) as this = 

    let changeMonitor = new ChangeWatcher()
    let settings = { Path = config.LocalPath; Pattern = "*.*" }
    let client = AlfrescoClient endPoint
    let mutable running = false

    member this.StartWatch() =
        if not running then
            changeMonitor.Watch settings this.ProcessChange
            running <- true

    member private this.ProcessChange(change) = 
        let fullPath = change.FullPath
        let fullLocalPath = FullLocalPath fullPath
        let localRoot = LocalRoot config.LocalPath
        let remoteRoot = RemoteRoot config.RemotePath
        let result = 
            match change.FileStatus with
            | Created -> 
                client.UploadFile remoteRoot localRoot fullLocalPath
            | Deleted -> 
                client.DeleteFile remoteRoot localRoot fullLocalPath
            | Renamed (old, n) -> 
                let info = 
                  { OldPath = old
                    NewPath = n }
                client.MoveFile remoteRoot localRoot info 
            | Changed -> 
                client.UploadFile remoteRoot localRoot fullLocalPath
        result |> ignore

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
        let config = SettingsManager.globalConfig()
        let endPoint = config.EndPoint
        let folders = config.Folders

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.StartTimer())