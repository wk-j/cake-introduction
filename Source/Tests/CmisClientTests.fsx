#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.CmisClient
open EasySyncClient.Utility
open EasySyncClient.Models

let go() =
    let folder = {
        LocalPath = "Resourc3"
        RemotePath = "/E-TAx"
    }

    let settings = {
        Url = "http://192.168.0.109:8080/alfresco"
        User = "admin"
        Password = "admin"
    }

    let client = CmisClient(settings, folder)
    client.OnMeetObject.Subscribe(fun x -> log "%A" x) |> ignore
    client.StartSync()

go()