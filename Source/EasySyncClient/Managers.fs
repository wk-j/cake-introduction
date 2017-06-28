module EasySyncClient.Managers
open System
open System.Threading
open System.IO
open EasySyncClient.Models
open EasySyncClient
open System.Linq
open System.Net
open System.Threading.Tasks
open EasySyncClient.FileWatcher
open EasySyncClient.ClientModels
open EasySyncClient.DB
open EasySyncClient.Utility
open EasySyncClient.CmisClient
open NLog

type ChangeManager(config : SyncFolder, cmis: CmisClient) = 
    let changeMonitor = new ChangeWatcher()
    let settings = { Path = config.LocalPath; Pattern = "*.*" }
    let mutable running = false

    member this.StartWatch() =
        if not running then
            changeMonitor.Watch settings this.TryProcessChange
            running <- true

    member private this.TryProcessChange(change) = 
        try 
            this.ProcessChange(change)
        with ex ->
            log "process change failed => %s => %s" ex.Message ex.StackTrace

    member private this.ProcessChange(change) = 
        let localPath = change.FullPath
        let remoteRoot = config.RemotePath
        let localRoot = config.LocalPath
        let relative = localPath.Replace(localRoot, "").TrimStart('/').TrimStart('\\').Replace("\\", "/")
        let targetPath = remoteRoot + "/" + relative

        let md5 { Md5 = md5 } = md5

        let result = 
            match change.FileStatus with
            | Created -> 
                Skip
            | Deleted -> 
                cmis.DeleteDocument targetPath
            | Renamed (old, n) -> 
                let newName = Path.GetFileName(n)
                cmis.Rename targetPath newName
            | Changed -> 
                Skip
        result |> ignore

type SyncManger() = 

    let timer = new Timers.Timer(10000.)

    member this.StartSync() = 

        let config = SettingsManager.globalConfig()
        //let endPoint = config.EndPoint
        let folders = config.Folders

        let endPoint = { Alfresco = "http://192.168.0.109:8080/alfresco"; User = "admin"; Password = "admin" }

        let folder0 = config.Folders.[0]

        let client = CmisClient(endPoint, folder0)

        //let change = ChangeManager(folder0, client)
        //change.StartWatch()

        let startSync x = 
            timer.Stop()
            client.DownSync()
            Thread.Sleep 10000
            client.UpSync()
            timer.Start()

        timer.Elapsed.Add(startSync)
        timer.Start()
        timer.AutoReset <- false