module EasySyncClient.CmisManager

open EasySyncClient.CmisClient
open EasySyncClient.Models
open EasySyncClient.ClientModels
open EasySyncClient.Utility
open EasySyncClient.DB
open System.IO

type CmisManager(settings, folder) =

    let client = CmisClient(settings, folder)

    let createFolder (RelativePath relative) = 
        let path = Path.Combine(folder.LocalPath, relative)
        if Directory.Exists path |> not then
            log "create local directory %s" path
            Directory.CreateDirectory path |> ignore

    let createFile (RelativePath relative) (FullPath fullPath) = 
        let localPath = Path.Combine(folder.LocalPath, relative)
        log "download file %s" relative
        client.DowloadDocument fullPath localPath

        let md5 = Utility.checkMd5 localPath
        let file = DbManager.queryFile localPath
        let newFile = { file with Md5 = md5 }
        DbManager.updateFile newFile |> ignore

    let handler data = 
        match data with
        | Folder (full, relative) -> createFolder relative
        | File (full, relative) -> createFile relative full

    member this.StartDownSync() =
        client.OnMeetObject.Subscribe(handler) |> ignore
        client.DownSync()

    member this.StartUpSync() = 
        client.UpSync()
