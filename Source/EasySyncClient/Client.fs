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

module List = 
    let apply (fl : ('a -> 'b) list) (xl : 'a list) =
        [ for f in fl do 
            for x in xl do yield f x ]

let inline (<*>) f xs = List.apply f xs

let splitWith (sep: string) (str: string) = str.Split([| sep |], StringSplitOptions.None)
let cleanPath (str: string) = str.TrimEnd('/').Replace("//", "/").Replace("\\", "/")
let toSections (data: string array) = [ for i in 0..data.Length do yield String.concat "/"  <| data.Take(i) ]
let replace (a:string) b (c: string)= c.Replace(a, b)

type RalativeRemotePath = RelativeRemotePath of string
type RelativeLocalPath = RelativeLocalPath of string
type RelativeRemoteNoName = RelativeRemoteNoName of string

type FullRemotePath = FullRemotePath of string
type FullRemotePathNoName = FullRemotePathNoName of string

type FullLocalPath = FullLocalPath of string

type RemoteSection = RemoteSection of string array
type RemoteRoot = RemoteRoot of string
type LocalRoot = LocalRoot of string

let createRelativePath (LocalRoot localRoot) (FullLocalPath localPath) = 
    let relative = replace localRoot "" localPath 
    let path = Path.GetDirectoryName relative
    let name = Path.GetFileName relative 
    relative |> RelativeRemotePath

let createSection (RelativeRemoteNoName path) =
    let path = path |> cleanPath
    splitWith "/" path |> toSections |> List.map RelativeRemotePath

let createFullRemotePath (RemoteRoot root) (RelativeRemotePath path)  = 
    root + "/" + path |> cleanPath  |> FullRemotePath

let createFullRemotePathNoName (RemoteRoot root) (RelativeRemoteNoName path) = 
    root + "/" + path |> cleanPath  |> FullRemotePathNoName

let extractName (FullRemotePath path) = 
    let last = path |> splitWith "/" |> Array.last
    let path = Path.GetDirectoryName path
    (path, last)

type Action  = 
    | CreateDir of RemoteRoot * RalativeRemotePath
    | CreateFile of RalativeRemotePath * RelativeLocalPath

type AlfrescoClient(endPoint) = 
    let logger = LogManager.GetCurrentClassLogger()

    let client = Client(NetworkCredential(UserName = endPoint.User, Password = endPoint.Password))
    do 
        client.Server <- endPoint.Url

    member this.ProcessAction = function
        | CreateDir (remoteRoot, remotePath) ->
            this.TryCreateDirectory remoteRoot remotePath
        | _ -> false

    member this.TryCreateDirectory remoteRoot remotePath = 
        let full = createFullRemotePath remoteRoot remotePath |> fun (FullRemotePath path) -> path

        async {
            do! Task.Delay(1000) |> Async.AwaitTask
            try 
                logger.Info("Try to create directory | {0}", full)
                let! items = client.GetFolder full |> Async.AwaitTask
                return true
            with ex ->
                try 
                    let path, last = extractName (FullRemotePath full)
                    logger.Info("Create {0} => {1}", path, last)
                    client.CreateDir(path, last) |> Async.AwaitTask |> ignore
                    return true
                with ex ->
                    return false
        } |> Async.RunSynchronously

    member this.TryCreateDirectories remoteRoot remotePath  =
        async {
            let full = createFullRemotePathNoName remoteRoot remotePath |> fun (FullRemotePathNoName path) -> path
            printfn "try to create directory %s" full
            try 
                let! item = client.GetFolder full |> Async.AwaitTask
                logger.Info("{0} is exists", full)
                return true
            with ex ->
                let sections = createSection remotePath 
                let rootMap = this.TryCreateDirectory remoteRoot
                sections |> List.map rootMap |> ignore
                return true
        } |> Async.RunSynchronously

    member this.UploadFile (RemoteRoot remoteRoot) (LocalRoot localRoot) (FullLocalPath localPath) = 
        if localPath.Contains localRoot then
            async {
                let path = createRelativePath (LocalRoot localRoot) (FullLocalPath localPath)
                let remote = RemoteRoot remoteRoot

                let getPath (RelativeRemotePath path) = Path.GetDirectoryName path |> RelativeRemoteNoName 

                this.TryCreateDirectories (RemoteRoot remoteRoot) (getPath path) |> ignore
                let targetPath = createFullRemotePath remote path

                let path, name = extractName targetPath
                
                printfn "try to upload file %s => %s" path name

                let! item =  client.Upload(path, File.OpenRead(localPath), name) |> Async.AwaitTask
                return item
            }
            |> Async.RunSynchronously
            |> ignore
            true
        else
            false

type FolderManager(endPoint, config) as this =

    let timer = new System.Timers.Timer(10000.0)    
    let client = AlfrescoClient(endPoint)

    do 
        timer.AutoReset <- false
        timer.Elapsed.Add(this.Process)

    member private this.Upload(file) = 
        let full, rel = file
        let fullPath = config.RemotePath
        let sections = Path.GetDirectoryName rel  |> splitWith "/"  |> toSections
        ()

    member private this.Process(args) =
        printfn "process ..."

        let local = config.LocalPath
        let pause = timer.Stop
        let resume = timer.Start
        let touch() = SettingsManager.touch config

        let startUpload (fileInfo: FileInfo) = 
            printfn "start upload %s" fileInfo.FullName
            let localRoot = LocalRoot config.LocalPath
            let remoteRoot = RemoteRoot config.RemotePath
            let full = FullLocalPath fileInfo.FullName
            client.UploadFile remoteRoot localRoot full

        let findModifyFiles() = 
            printfn "file modified files %s" config.LocalPath
            let last = SettingsManager.getTouchDate (config)
            Directory.EnumerateFiles(local, "*.txt", SearchOption.AllDirectories)
                |> Seq.map FileInfo
                |> Seq.filter(fun x -> x.LastWriteTime >= last)
        pause()

        let files = findModifyFiles() 
        let results = files |> Seq.map startUpload |> Seq.toList

        touch()
        resume()

    member this.Start = timer.Start
    member this.Stop = timer.Stop

    interface IDisposable with
        member this.Dispose() = timer.Dispose()

type SyncManger() = 

    member this.CreateFolderManager(endPoint, folder) = 
        new FolderManager(endPoint, folder)

    member this.StartSync() = 
        let config = SettingsManager.localConfig()
        let endPoint = config.EndPoint
        let folders = config.Folders

        let folderManagers = 
            folders 
            |> List.map (fun x -> this.CreateFolderManager(endPoint, x))

        folderManagers |> List.iter (fun x -> x.Start())