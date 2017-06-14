module EasySyncClient

open WebDAVClient
open System
open System.Threading
open Newtonsoft.Json
open System.IO

type EndPoint = {
    Url: string
    User: string
    Password: string
}

type SyncFolders = {
    Folders : string list
}

type SyncFolder = {
    RemotePath : string
    LastCheck : DateTime
}

module SettingsManger = 
    let private loadHomeSetting fileName (defaultObject: 'a) = 
        let dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        let file = Path.Combine(dir, fileName)
        if File.Exists file then
            let content = File.ReadAllText file
            JsonConvert.DeserializeObject<'a>(content)
        else
            let json = JsonConvert.SerializeObject(defaultObject)
            File.WriteAllText(file, json)
            defaultObject

    let loadEndPoint() = 
        let endPoint = { 
            Url = "http://192.168.0.109:8080"
            User = "admin"
            Password = "admin"
        }
        loadHomeSetting ".easy-sync-endpoint" endPoint

    let loadFolders() =
        let folders = {
            Folders = []
        }
        loadHomeSetting ".easy-sync-folders" folders

type SyncManager(endPoint, folder) as this =

    let timer = new System.Timers.Timer(1000.0)    
    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member private this.Process(args) =
        timer.Stop()
        printfn "Hello"
        timer.Start()

    member this.Start() =
        timer.Start()

    member this.Stop() =
        timer.Stop()

    interface IDisposable with
        member this.Dispose() =
            timer.Dispose()