module EasySyncClient.SettingsManager 

open System
open System.IO
open EasySyncClient.Models
open Newtonsoft.Json
open System.Linq
open EasySyncClient.Utility

let private dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

let private configPath = function
    | Global -> Path.Combine(dir, ".easy-sync-global")

let globalConfig() =
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
