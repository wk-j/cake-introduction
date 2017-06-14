module EasySyncClient.Client

open WebDAVClient
open System
open System.Threading
open Newtonsoft.Json
open System.IO
open EasySyncClient.Models
open EasySyncClient

type FolderManager(endPoint, folder) as this =

    let timer = new System.Timers.Timer(1000.0)    
    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member private this.Process(args) =
        timer.Stop()
        printfn "Hello"
        timer.Start()

    member this.Start() =
        timer.Start()

    member this.Stop() =
        timer.Stop()

    interface IDisposable with
        member this.Dispose() =
            timer.Dispose()

type SyncManger() = 

    member this.CreateFolderManager(endPoint, folder) = 
        new FolderManager(endPoint, folder)

    member this.StartSync() = 
        let endPoint = SettingsManager.loadEndPoint()

        let folders = 
            SettingsManager.loadFolders().Folders 
            |> List.map (SettingsManager.loadFolder)
            |> List.choose id

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.Start())

        ()