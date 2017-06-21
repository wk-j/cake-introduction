#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.Managers
open EasySyncClient.Models
open EasySyncClient.SettingsManager
open System

let endPoint = {
    Url = "http://192.168.0.109:8080"
    User = "admin"
    Password = "admin"
}

let folder = { 
    RemotePath = "/alfresco/webdav/Validate"
    LocalPath = "/Users/wk/Source/project/easy-sync/EasySyncClient/Resource"
}

let start() =
    let mgr = new FolderManager(endPoint, folder)
    mgr.StartTimer()

    Console.ReadLine()