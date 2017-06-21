module EasySyncClient.UploadManager

open EasySyncClient.AlfrescoClient
open EasySyncClient.ClientModels
open EasySyncClient.Models
open EasySyncClient.DB
open System

let processFile (client: AlfrescoClient) (folder)  file = 

    let action : FileAction = file.FileAction
    let localPath = file.OriginalPath
    let fullLocalPath = FullLocalPath localPath
    let remoteRoot = RemoteRoot folder.RemotePath
    let localRoot = LocalRoot folder.LocalPath

    let results =
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
    (results)

let start settings (folder: SyncFolder) =

    let alfresco = AlfrescoClient(settings)
    let action = processFile alfresco folder

    let file = DbManager.queryFile Status.Initialize DateTime.MinValue
    file |> Seq.toList |> List.map action