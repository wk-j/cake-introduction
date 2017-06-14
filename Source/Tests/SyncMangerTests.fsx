
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.Client
open System

let manager = SyncManger()
manager.StartSync()

Console.ReadLine()
