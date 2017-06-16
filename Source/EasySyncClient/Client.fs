module EasySyncClient.Client

open WebDAVClient
open System
open System.Threading
open Newtonsoft.Json
open System.IO
open EasySyncClient.Models
open EasySyncClient
open System.Linq
open System.Net
open WebDAVClient
open System.Threading.Tasks
open NLog

let splitWith (sep: string) (str: string) = str.Split([| sep |], StringSplitOptions.None)
let cleanPath (str: string) = str.TrimEnd('/').Replace("//", "/").Replace("\\", "/")
let toSections (data: string array) = [ for i in 0..data.Length do yield String.concat "/"  <| data.Take(i) ]

type AlfrescoClient(endPoint) = 
    let logger = LogManager.GetCurrentClassLogger()

    let client = Client(NetworkCredential(UserName = endPoint.User, Password = endPoint.Password))
    do 
        client.Server <- endPoint.Url

    member this.TryCreateDirectory root section = 
        let full = root + "/" + section |> cleanPath
        async {
            do! Task.Delay(1000) |> Async.AwaitTask
            try 
                logger.Info("Try to create directory | {0}", full)
                let! items = client.GetFolder full |> Async.AwaitTask
                return true
            with ex ->
                try 
                    let last = full |> splitWith "/" |> Array.last
                    let path = Path.GetDirectoryName full
                    logger.Info("Create {0} => {1}", path, last)
                    client.CreateDir(path, last) |> Async.AwaitTask |> ignore
                    return true
                with ex ->
                    return false
        } |> Async.RunSynchronously

    member this.TryCreateDirectories root section =
        async {
            let full = root + "/" + section |> cleanPath
            try 
                let! item = client.GetFolder full |> Async.AwaitTask
                logger.Info("{0} is exists", full)
                return true
            with ex ->
                let sections = section |> splitWith "/" |> toSections
                let rootMap = this.TryCreateDirectory root
                sections |> List.map rootMap |> ignore
                return true
        } |> Async.RunSynchronously

    member this.UploadFile (remoteRoot: string) (localRoot: string) (localFile: string) = 
        let replace (a:string) b (c: string)= c.Replace(a, b)

        if localFile.Contains localRoot then
            let relative = replace localRoot "" localFile 
            let path = Path.GetDirectoryName relative
            let name = Path.GetFileName relative 

            printfn "%s" relative
            printfn "%s" name
            printfn "%s"  path

            this.TryCreateDirectories remoteRoot path |> ignore
            let targetPath = remoteRoot + "/" + path |> cleanPath

            async {
                let! item =  client.Upload(targetPath, File.OpenRead(localFile), name) |> Async.AwaitTask
                return item
            }
            |> Async.RunSynchronously
            |> ignore

            true

        else
            false

type FolderManager(endPoint, config) as this =

    let timer = new System.Timers.Timer(5000.0)    

    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member private this.Upload(file) = 
        let full, rel = file
        let fullPath = config.RemotePath
        let sections = Path.GetDirectoryName rel  |> splitWith "/"  |> toSections
        ()

    member private this.Process(args) =
        let local = config.LocalPath

        let pause() = 
            timer.Stop()

        let touch() = 
            SettingsManager.touceFolderConfig  local

        let toRelative (file: string) =
            file.Replace(local, "")

        let findModifyFiles() = 
            let last = SettingsManager.getTouchDate local 
            Directory.EnumerateFiles(local, "*.txt")
                |> Seq.map FileInfo
                |> Seq.filter(fun x -> x.LastWriteTime >= last)
                |> Seq.map(fun x -> (x, x.FullName |> toRelative))

        let resume() =
            timer.Start()

        pause()

        let files = findModifyFiles() 
        //touch()
        resume()

    member this.Start() =
        timer.Start()

    member this.Stop() =
        timer.Stop()

    interface IDisposable with
        member this.Dispose() =
            timer.Dispose()

type SyncManger() = 

    member this.CreateFolderManager(endPoint, folder) = 
        new FolderManager(endPoint, folder)

    member this.StartSync() = 
        let endPoint = SettingsManager.loadEndPoint()

        let folders = 
            SettingsManager.loadFolders().Folders 
            |> List.map (SettingsManager.loadFolderConfig)
            |> List.choose id

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.Start())