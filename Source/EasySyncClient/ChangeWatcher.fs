module EasySyncClient.FileWatcher

open System.IO
open System.Linq
open System.Collections.Generic
open System.Timers
open System
open EasySyncClient.Utility
open System.Threading

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

    let mutable processing = false
    let timer = new System.Timers.Timer(50.)
    let unNotifiedChanages = List<FileChange>()
    let mutable watcher = new FileSystemWatcher()

    interface IDisposable with
        member this.Dispose() =
            watcher.EnableRaisingEvents <- false
            watcher.Dispose()
            timer.Dispose()

    member private this.AcumChanges fileChange = 
        // log "change => %s => %A" fileChange.FullPath fileChange.FileStatus
        if not processing then
            timer.Start()
            unNotifiedChanages.Add fileChange

    member private this.HandleWatcherEvent (onChange: FileChange -> unit ) status (e: FileSystemEventArgs) =
        { FullPath = e.FullPath
          Name = e.Name
          FileStatus = status } |> onChange

    member private this.HandleRenameEvent (onChange: FileChange -> unit )  (e: RenamedEventArgs) =
        let status = Rename (e.OldName,e.Name)
        { FullPath = e.FullPath
          Name = e.Name
          FileStatus =  status } |> onChange

    member private this.Start settings = 

        log "path = %s" settings.Path
        log "pattern = %s" settings.Pattern

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

        log "watch %s" settings.Path

        let full { FullPath = full } = full
        let status { FileStatus = status } = status
        let first (full,changes) = changes |> Seq.sortBy status |> Seq.head

        timer.AutoReset <- false
        timer.Elapsed.Add(
            fun  e ->
                processing <- true
                if unNotifiedChanages.Any() then
                    let changes = unNotifiedChanages |> Seq.groupBy full |> Seq.map (first) |> Seq.toList
                    try
                        log "process => %d" <| changes.Count()  
                        changes |> List.iter onChange
                    with ex ->
                        log "error => %s" ex.Message
                    unNotifiedChanages.Clear()
                processing <- false
        )

        this.Start settings
