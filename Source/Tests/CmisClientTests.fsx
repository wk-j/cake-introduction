#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.CmisClient
open EasySyncClient.Utility

let go() =
    let client = CmisClient("/E-Tax", "Resource")
    client.OnMeetObject.Subscribe(fun x -> log "%A" x) |> ignore
    client.StartSync()

go()