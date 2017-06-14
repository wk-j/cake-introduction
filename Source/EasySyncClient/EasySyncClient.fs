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

type ConfigFile = 
    | EndPoint
    | Folders
    | Config of String

module SettingsManger = 
    let dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    let configPath = function
        | EndPoint -> Path.Combine(dir, ".easy-sync-endpoint")
        | Folders -> Path.Combine(dir, ".easy-sync-folders")
        | Config path -> Path.Combine(path, ".easy-sync-config")

    let private writeConfig path object = 
        let path = configPath path
        let json = JsonConvert.SerializeObject(object)
        File.WriteAllText(path, json)

        printfn "Path = %s" path
        printfn "Content = %s" json

    let private loadHomeSetting (path:ConfigFile) (defaultObject: 'a) = 
        let fullPath = configPath path
        if File.Exists fullPath then
            let content = File.ReadAllText fullPath
            JsonConvert.DeserializeObject<'a>(content)
        else
            writeConfig path defaultObject
            defaultObject

    let saveFolders folder = 
        ()

    let loadEndPoint() = 
        let endPoint = { 
            Url = "http://192.168.0.109:8080"
            User = "admin"
            Password = "admin"
        }

        loadHomeSetting EndPoint endPoint

    let loadFolder folder = 
        let configFile = configPath (Config folder)
        if File.Exists configFile then
            let content = File.ReadAllText configFile
            JsonConvert.DeserializeObject<SyncFolder>(content) |> Some
        else
            None

    let loadFolders() =
        let folders = {
            Folders = []
        }
        loadHomeSetting Folders folders

    let addFolder path = 
        let folder = loadFolders()
        let newFolder = { folder with Folders = path :: folder.Folders }
        writeConfig Folders newFolder

type FolderManager(endPoint, folder) as this =

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

type SyncManger() = 

    member this.CreateFolderManager(endPoint, folder) = 
        new FolderManager(endPoint, folder)

    member this.StartSync() = 
        let endPoint = SettingsManger.loadEndPoint()

        let folders = 
            SettingsManger.loadFolders().Folders 
            |> List.map (SettingsManger.loadFolder)
            |> List.choose id

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.Start())

        ()