module EasySyncClient.Models

open System

type EndPoint = {
    Url: string
    User: string
    Password: string
}

type SyncFolders = {
    Folders : string list
}

type SyncFolder = {
    LocalPath : string
    RemotePath : string
}

type ConfigFile = 
    | EndPoint
    | Folders
    | Config of String