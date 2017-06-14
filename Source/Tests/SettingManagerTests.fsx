#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient

let endPoint = SettingsManger.loadEndPoint()
let folders = SettingsManger.loadFolders()

printfn "EndPoint = %A" endPoint
printfn "Folders = %A" folders