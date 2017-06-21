#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient
open EasySyncClient.Managers

let localPath = "/Users/wk/Source/project/easy-sync/EasySyncClient/Resource"
let remotePath = "/alfresco/webdav/Validate"

let config = SettingsManager.localConfig()

SettingsManager.writeConfig config