#r "../../packages/WebDAVClient/lib/net45/WebDAVClient.dll"

open WebDAVClient
open System.Net
open System.IO
open System.Threading.Tasks

let client = Client(NetworkCredential(UserName = "admin", Password = "admin"))
client.Server <- "http://192.168.0.109:8080"

let listFile() =
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

let uploadFile() =
    client.BasePath <- "/alfresco/webdav/Validate"

    let resource = "./Resource"

    let uploadFile (folder: Model.Item) file = 
        async { 
            let info = FileInfo(file)
            let! rs = client.Upload(folder.Href, File.OpenRead(file), info.Name) |> Async.AwaitTask
            return rs
        } |> Async.RunSynchronously

    async {
        let! folder = client.GetFolder("/alfresco/webdav/Validate") |> Async.AwaitTask
        let files = Directory.EnumerateFiles(resource, "*.*", SearchOption.AllDirectories) |> Seq.toList
        let uploader = uploadFile folder
        let result = files |> List.map uploader |> List.toArray
        return result
    }  |> Async.RunSynchronously


uploadFile() |> printfn "%A"