module EasySyncClient.DownloadManager

open EasySyncClient.CmisClient
open EasySyncClient.Models
open EasySyncClient.ClientModels
open EasySyncClient.Utility
open System.IO

type DownloadManager(settings, folder) =

    let client = CmisClient(settings, folder)

    let createFolder (RelativePath relative) = 
        let path = Path.Combine(folder.LocalPath, relative)
        if Directory.Exists path then
            ()
        else
            log "create local directory %s" path
            Directory.CreateDirectory path |> ignore

    let handler data = 
        match data with
        | Folder (full, relative) -> createFolder(relative)
        | File (full, relative) -> ()

    member this.Start() =
        client.OnMeetObject.Subscribe(handler) |> ignore
        client.StartSync()






