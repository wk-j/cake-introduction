#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.UploadManager
open EasySyncClient.Models
open System


let start() =
    let config = {
        EndPoint.Url = "http://192.168.0.109:8080"
        User = "admin"
        Password =" admin" }

    let folder = {
        SyncFolder.LocalPath = "Source"
        RemotePath = "/alfresco/webdav/Validate"
    }

    UploadManager.start config
    Console.ReadLine()

// start()