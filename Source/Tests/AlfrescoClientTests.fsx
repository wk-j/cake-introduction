
#r "../../packages/NLog/lib/net45/NLog.dll"
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.Client

let client = AlfrescoClient( { User = "admin"; Password = "admin"; Url = "http://192.168.0.109:8080"})

let baseDir = "/alfresco/webdav/Validate"

let createDir() =
    client.TryCreateDirectories baseDir  "SecA/SecB/SecC" |> printfn "%A"

let uploadFile() =
    let localFile = "Resource/Dir1/Dir2/Dir3/Test1.txt"
    let localPath = "Resource"

    client.UploadFile baseDir localPath localFile


uploadFile() |> printfn "%A"
