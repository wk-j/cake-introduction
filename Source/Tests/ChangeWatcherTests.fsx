#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.FileWatcher
open EasySyncClient.Utility
open System

let settings = {
    Path = "/Users/wk/Source/project/easy-sync/EasySyncClient/Resource"
    Pattern = "*.txt"
}

let watcher = new ChangeWatcher()
watcher.Watch settings (fun x -> log "==== %A %A" x.FullPath x.FileStatus)

Console.ReadLine()