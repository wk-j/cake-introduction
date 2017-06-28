module EasySyncClient.Console

open EasySyncClient.Managers
open EasySyncClient.Utility
open EasySyncClient.SettingsManager
open EasySyncClient.Models
open EasySyncClient.Utility

let fix x y = x
let read = System.Console.ReadLine

[<EntryPoint>]
let main argv =

    let config = globalConfig()
    writeConfig config

    log "config path -- %s" (configPath Global)
    log "press [enter] to start"
    read()  |> ignore

    let manager = SyncManger()
    manager.StartSync()

    read() |> fix 0