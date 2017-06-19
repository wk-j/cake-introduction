module EasySyncClient.SettingsManager 

open System
open System.IO
open EasySyncClient.Models
open Newtonsoft.Json
open System.Linq

let private dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

let private configPath = function
    | Global -> Path.Combine(dir, ".easy-sync-global")
    | Local path -> Path.Combine(path, ".easy-sync-local")

let localConfig() =
    let file = configPath Global

    if File.Exists file then
        let text = File.ReadAllText(file)
        JsonConvert.DeserializeObject<Config>(text)
    else
        { Config.Folders = []
          EndPoint = 
            { Url = "http://192.168.0.109:8080"
              User = "admin"
              Password = "admin" } }

let writeConfig config = 
    let file = configPath Global
    let json = JsonConvert.SerializeObject(config)
    File.WriteAllText (file, json)

let touch { SyncFolder.LocalPath = local } = 
    let path = configPath (Local local)
    if File.Exists path then
        printfn "touch file %s" path
        File.SetLastWriteTime(path, DateTime.Now)
    else
        File.WriteAllText(path, "")

let getTouchDate { SyncFolder.LocalPath = local } = 
    let path = configPath (Local local)
    if File.Exists path then
        File.GetLastWriteTime (path)
    else 
        System.DateTime.MinValue