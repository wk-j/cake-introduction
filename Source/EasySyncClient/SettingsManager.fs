module EasySyncClient.SettingsManager 

open System
open System.IO
open EasySyncClient.Models
open Newtonsoft.Json
open System.Linq
open EasySyncClient.Utility

let private dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".EasySync")

if Directory.Exists dir |> not then
    Directory.CreateDirectory dir |> ignore

let configPath = function
    | Global -> Path.Combine(dir, "EasySync.json")
    | DB -> Path.Combine(dir, "EasySync.db")

let globalConfig() =
    let file = configPath Global

    if File.Exists file then
        let text = File.ReadAllText(file)
        JsonConvert.DeserializeObject<Config>(text)
    else
        { Config.Folders = []
          EndPoint = 
            { Alfresco = "http://192.168.0.109:8080/alfresco"
              User = "admin"
              Password = "admin" } }

let writeConfig config = 
    let file = configPath Global
    let json = JsonConvert.SerializeObject(config, Formatting.Indented)
    File.WriteAllText (file, json)
