#r "../../packages/Apache.DotCMIS/lib/DotCMIS.dll"
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.CmisClient
open EasySyncClient.Utility
open EasySyncClient.Models
open EasySyncClient.ClientModels

let folder = {
    LocalPath = "Resourc3"
    RemotePath = "/E-TAx"
}

let settings = {
    Url = "http://192.168.0.109:8080/alfresco"
    User = "admin"
    Password = "admin"
}

let go() =
    let client = CmisClient(settings, folder)
    client.OnMeetObject.Subscribe(fun x -> log "%A" x) |> ignore
    client.StartSync()

let createFolder() =
    let client = CmisClient(settings, folder)
    let relative = "/Validate/YYY/BBB/CCC/DDD/EEE"
    let rs = client.CreateFolders relative
    rs |> printfn "%A"

let createDocument() =
    let client = CmisClient(settings, folder)
    let target = "/Validate/KKK/KKK/Hello.txt"
    let rs = client.UpdateDocument target "Resource/Test1.txt"
    rs |> printfn "%A"

let updateDocument() =
    let client = CmisClient(settings, folder)
    let target = "/Validate/KKK/KKK/Hello.txt"
    let rs = client.UpdateDocument target "Resource/Test1.txt"
    rs |> printfn "%A"

let downloadDocument() =
    let client = CmisClient(settings, folder)
    let target = "/Validate/KKK/KKK/Hello.txt"
    client.DowloadDocument target "Resource/KKK.txt"

downloadDocument()
//updateDocument()
//createFolder()
//createDocument()