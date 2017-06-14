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
    RemotePath : string
    LastCheck : DateTime
}

type ConfigFile = 
    | EndPoint
    | Folders
    | Config of String

