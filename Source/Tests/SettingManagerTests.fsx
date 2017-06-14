#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.Client
open EasySyncClient

let endPoint = SettingsManager.loadEndPoint()
let folders = SettingsManager.loadFolders()

printfn "EndPoint = %A" endPoint
printfn "Folders = %A" folders