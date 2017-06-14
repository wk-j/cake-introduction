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
    LastCheck = DateTime.MinValue
}

let mgr = new FolderManager(endPoint, folder)
mgr.Start()

Console.ReadLine()