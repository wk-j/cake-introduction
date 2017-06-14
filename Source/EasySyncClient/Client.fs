module EasySyncClient.Client

open WebDAVClient
open System
open System.Threading
open Newtonsoft.Json
open System.IO
open EasySyncClient.Models
open EasySyncClient
open System.Linq

type FolderManager(endPoint, config) as this =

    let timer = new System.Timers.Timer(5000.0)    
    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member private this.Process(args) =
        let local = config.LocalPath

        let pause() = 
            timer.Stop()

        let touch() = 
            printfn "%A" DateTime.Now
            SettingsManager.touceFolderConfig  local

        let findModifyFiles() = 
            let last = SettingsManager.getTouchDate local 
            printfn "%s" local
            Directory.EnumerateFiles(local, "*.txt")
                |> Seq.map FileInfo
                |> Seq.filter(fun x -> x.LastWriteTime >= last)

        let toRelative (file: string) =
            file.Replace(local, "")

        let resume() =
            timer.Start()

        pause()

        let files = findModifyFiles() 
        let relative = files |> Seq.map (fun x -> x.FullName |> toRelative) 
        printfn "%A" <| files.ToList()
        printfn "%A" relative

        //touch()
        resume()

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
            |> List.map (SettingsManager.loadFolderConfig)
            |> List.choose id

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.Start())