
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient
open System

let manager = SyncManger()
manager.StartSync()

Console.ReadLine()
