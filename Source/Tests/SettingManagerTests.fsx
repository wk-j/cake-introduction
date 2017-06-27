#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.SettingsManager
open EasySyncClient
open EasySyncClient.Models


let config = SettingsManager.globalConfig()

let syncFolder = {
    SyncFolder.LocalPath = "/Users/wk/Source/project/easy-sync/EasySyncClient/Resource"
    RemotePath = "/Validate"
}

let newConfig = { config with Folders = [syncFolder] }

SettingsManager.writeConfig newConfig

printfn "EndPoint = %A" newConfig