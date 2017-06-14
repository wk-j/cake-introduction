#r "../../packages/WebDAVClient/lib/net45/WebDAVClient.dll"

open WebDAVClient
open System.Net

let client = Client(NetworkCredential(UserName = "admin", Password = "admin"))

client.Server <- "http://192.168.0.109:8080"
client.BasePath <- "/alfresco/webdav/Validate"

let files = 
    async { 
        let! files = client.List() |> Async.AwaitTask
        return files
    } |> Async.RunSynchronously

let list (file: Model.Item) =
    printfn "%A" file.DisplayName
    printfn "%A" file.Etag

files |> Seq.iter list

