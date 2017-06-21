#r "../../packages/NLog/lib/net45/NLog.dll"
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.AlfrescoClient
open EasySyncClient.ClientModels

let client = AlfrescoClient( { User = "admin"; Password = "admin"; Url = "http://192.168.0.109:8080"})

let baseDir = "/alfresco/webdav/Validate"
let root = RemoteRoot baseDir

let createDir() =
    let relative = RemoteRelativeNoName "SecA/SecB/SecC"
    client.TryCreateDirectories root  relative |> printfn "%A"

let uploadFile() =
    let localFile = "Resource/Dir1/Dir2/Dir3/Test1.txt"
    let localPath = "Resource"

    let localRoot = LocalRoot localPath
    let fullPath = FullLocalPath localFile

    client.UploadFile root localRoot fullPath

let renameFile() =
    let info = 
        { MoveInfo.OldPath = "Resource/Dir1/Dir2/Dir3/Test1.txt"
          NewPath = "Resource/Dir1/Dir2/Dir3/Test1-New.txt" }

    let localPath = "Resource"
    let localRoot = LocalRoot localPath

    client.MoveFile root localRoot info

//uploadFile() |> printfn "%A"
//renameFile() |> printfn "%A"