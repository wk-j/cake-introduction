#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.SettingsManager
open EasySyncClient


let endPoint = SettingsManager.localConfig()

printfn "EndPoint = %A" endPoint