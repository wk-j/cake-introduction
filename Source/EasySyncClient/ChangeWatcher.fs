module EasySyncClient.FileMonitor

open System.IO
open System.Linq
open System.Collections.Generic
open System.Timers
open System

type FileStatus = 
    | Deleted
    | Created
    | Changed
    | Rename of string * string

type FileChange = {
    FullPath : string
    Name: string
    FileStatus : FileStatus
}

type WatchSettings = {
    Pattern : string
    Path: string
}

type ChangeWatcher()  =

    let mutable runningHandler = false
    let timer = new Timer(50.)
    let unNotifiedChanages = List<FileChange>()
    let mutable watcher = new FileSystemWatcher()

    interface IDisposable with
        member this.Dispose() =
            watcher.EnableRaisingEvents <- false
            watcher.Dispose()
            timer.Dispose()

    member private this.AcumChanges fileChange = 
        if not runningHandler then
            unNotifiedChanages.Add fileChange
            timer.Start()

    member private this.HandleWatcherEvent (onChange: FileChange -> unit ) status (e: FileSystemEventArgs) =
        { FullPath = e.FullPath
          Name = e.Name
          FileStatus = status } |> onChange

    member private this.HandleRenameEvent (onChange: FileChange -> unit )  (e: RenamedEventArgs) =
        let status = Rename (e.OldName,e.Name)
        { FullPath = e.FullPath
          Name = e.Name
          FileStatus =  status } |> onChange

    member private this.Start settings (onChange : FileChange -> unit)= 
        let full = DirectoryInfo(settings.Path).FullName
        watcher <- new FileSystemWatcher(full, settings.Pattern)
        watcher.EnableRaisingEvents <- true
        watcher.IncludeSubdirectories <- true

        let change = this.HandleWatcherEvent this.AcumChanges
        let rename = this.HandleRenameEvent this.AcumChanges

        watcher.Changed.Add(change FileStatus.Changed)
        watcher.Created.Add(change FileStatus.Created)
        watcher.Deleted.Add(change FileStatus.Deleted)
        watcher.Renamed.Add(rename)

    member this.Watch settings (onChange: FileChange -> unit) =
        let full { FullPath = full } = full
        let status { FileStatus = status } = status
        let first (full,changes) = changes |> Seq.sortBy status |> Seq.head

        timer.AutoReset <- false

        timer.Elapsed.Add(
            fun  e ->
                if unNotifiedChanages.Any() then
                    let changes = unNotifiedChanages |> Seq.groupBy full |> Seq.map (first)
                    unNotifiedChanages.Clear()

                    try
                        runningHandler <- true
                        changes |> Seq.iter onChange
                    with ex ->
                        runningHandler <- true
        )

        this.Start settings