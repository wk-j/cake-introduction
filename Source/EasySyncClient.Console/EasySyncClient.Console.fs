module EasySyncClient.Console

open EasySyncClient.Managers
open EasySyncClient.Utility

[<EntryPoint>]
let main argv =
    let manager = SyncManger()
    manager.StartSync()
    while System.Console.ReadLine() = "x" do
        log "..."
    0 // return an integer exit code
