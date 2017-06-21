module EasySyncClient.UploadManager

open EasySyncClient.AlfrescoClient
open EasySyncClient.ClientModels
open EasySyncClient.Models
open EasySyncClient.DB
open EasySyncClient.Utility
open System
open System.Timers

module UploadManager = 

    let private processFile (client: AlfrescoClient) (folder)  file = 
        log "process file %s" file.OriginalPath

        let action : FileAction = file.FileAction
        let localPath = file.OriginalPath
        let fullLocalPath = FullLocalPath localPath
        let remoteRoot = RemoteRoot folder.RemotePath
        let localRoot = LocalRoot folder.LocalPath

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
            let newFile = { file with Status = Status.ProcessSuccess }
            DbManager.updateFile newFile
        else
            let newFile = { file with Status = Status.ProcessFailed }
            DbManager.updateFile newFile

    let start settings (folder: SyncFolder) =
        let alfresco = AlfrescoClient(settings)
        let action = processFile alfresco folder

        let timer = new Timer(3000.0)

        let handler x = 
            log "query files ..."
            let file = DbManager.queryFile Status.Initialize DateTime.MinValue
            let rs = file |> Seq.toList |> List.map action
            timer.Start()

        timer.AutoReset <- false
        timer.Enabled <- true
        timer.Elapsed.Add(handler)