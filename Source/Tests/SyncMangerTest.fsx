#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient
open System

let endPoint = {
    Url = "http://192.168.0.109:8080"
    User = "admin"
    Password = "admin"
}

let folder = { 
    RemotePath = "/alfresco/webdav/Validate"
    LocalPath = "./Resource"
    LastCheck = DateTime.MinValue
}

let mgr = new SyncManager(endPoint, folder)
mgr.Start()

Console.ReadLine()