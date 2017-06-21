module EasySyncClient.UploadManager

open EasySyncClient.AlfrescoClient
open EasySyncClient.ClientModels
open EasySyncClient.Models
open EasySyncClient.DB
open EasySyncClient.Utility
open System
open System.Timers

module UploadManager = 

    let private processFile (client: AlfrescoClient) file = 
        log "process file %s" file.OriginalPath

        let action : FileAction = file.FileAction
        let localPath = file.OriginalPath
        let fullLocalPath = FullLocalPath localPath
        let remoteRoot = RemoteRoot file.RemoteRoot 
        let localRoot = LocalRoot file.LocalRoot 

        let success =
            match action with
            | FileAction.Created ->
                client.UploadFile remoteRoot localRoot fullLocalPath
            | FileAction.Deleted ->
                client.DeleteFile remoteRoot localRoot fullLocalPath
            | FileAction.Changed ->
                client.UploadFile remoteRoot localRoot fullLocalPath
            | FileAction.Renamed 
            | FileAction.Moved ->
                let info = { MoveInfo.OldPath = localPath; NewPath = file.NewPath }
                client.MoveFile remoteRoot localRoot info
            | _ -> false

        if success then
            log "sync success %s" file.OriginalPath
            let newFile = { file with Status = Status.ProcessSuccess }
            DbManager.updateFile newFile
        else
            log ">> sync failed %s" file.OriginalPath
            let newFile = { file with Status = Status.ProcessFailed }
            DbManager.updateFile newFile

    let start settings =
        let alfresco = AlfrescoClient(settings)
        let action = processFile alfresco

        let timer = new Timer(3000.0)

        let handler x = 
            let file = DbManager.queryFile Status.Initialize DateTime.MinValue
            let rs = file |> Seq.toList |> List.map action
            timer.Start()

        timer.AutoReset <- false
        timer.Enabled <- true
        timer.Elapsed.Add(handler)