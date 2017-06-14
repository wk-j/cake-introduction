module EasySyncClient.SettingsManager 

open System
open System.IO
open EasySyncClient.Models
open Newtonsoft.Json
open System.Linq

let private dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

let private configPath = function
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

let loadEndPoint() = 
    let endPoint = { 
        Url = "http://192.168.0.109:8080"
        User = "admin"
        Password = "admin"
    }

    loadHomeSetting EndPoint endPoint

let touceFolderConfig path = 
    let path = configPath (Config path)
    File.SetLastWriteTime(path, DateTime.Now)

let getTouchDate path = 
    let path = configPath (Config path)
    File.GetLastWriteTime(path)

let writeFolderConfig config = 
    writeConfig (Config config.LocalPath) config

let loadFolderConfig path = 
    let configFile = configPath (Config path)
    if File.Exists configFile then
        let content = File.ReadAllText configFile
        let folder = JsonConvert.DeserializeObject<SyncFolder>(content) 
        { folder with LocalPath = path }
        |> Some
    else
        None

let loadFolders() =
    let folders = {
        Folders = []
    }
    loadHomeSetting Folders folders

let optimizeFolder() = 
    let folder = loadFolders()
    let set = folder.Folders |> Set.ofList
    let nq = set |> Set.toList
    let newFolder = { folder with Folders = nq }
    writeConfig Folders newFolder

let addFolder path = 
    let folder = loadFolders()

    if not <| folder.Folders.Contains path then
        let newFolder = { folder with Folders = path :: folder.Folders }
        writeConfig Folders newFolder