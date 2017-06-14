module EasySyncClient

open WebDAVClient
open System
open System.Threading

type EndPoint = {
    Url: string
    User: string
    Password: string
}

type SyncFolder = {
    RemotePath : string
    LocalPath : string
    LastCheck : DateTime
}

type SyncManager(endPoint, folder) as this =

    let timer = new System.Timers.Timer(1000.0)    
    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member this.Process(args) =
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


