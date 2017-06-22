
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.Managers
open System

let start() =
    let manager = SyncManger()
    manager.StartSync()
    Console.ReadLine()

start()