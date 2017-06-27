module EasySyncClient.Models

open System

type EndPoint = {
    Url: string
    User: string
    Password: string
}

type SyncFolder = {
    LocalPath : string
    RemotePath : string
}

type Config = {
    EndPoint : EndPoint
    Folders: SyncFolder list
}

type ConfigFile = 
    | Global