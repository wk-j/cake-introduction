#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.DownloadManager
open EasySyncClient.Models
open System.IO

let start() =
    let settings = {
        Url = "http://192.168.0.109:8080/alfresco"
        User = "admin"
        Password = "admin"
    }
    let folder = {
        LocalPath = DirectoryInfo("Resource").FullName
        RemotePath = "/Validate"
    }
    let mgr = DownloadManager(settings, folder)
    mgr.StartUpSync()

start()