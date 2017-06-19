#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient
open EasySyncClient.Client

let localPath = "/Users/wk/Source/project/easy-sync/EasySyncClient/Resource"
let remotePath = "/alfresco/webdav/Validate"

let config = SettingsManager.localConfig()

SettingsManager.writeConfig config