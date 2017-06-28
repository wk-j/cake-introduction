module EasySyncClient.DB

open System
open LiteDB
open EasySyncClient.Utility
open System.Linq
open EasySyncClient.Models

type FileAction = 
    | Created = 0
    | Moved = 1
    | Renamed = 2
    | Deleted = 3
    | Changed = 4

[<CLIMutable>]
type QFolder = {
    Id : int
    RemoteRoot : string
    RelativePath : string
}

[<CLIMutable>]
type QFile = {
    Id : int
    RemoteRoot : string
    RelativePath : string
    Md5 : string
}

[<CLIMutable>]
type QTouch = {
    Id : int
    LastTouchTime : DateTime
    LocalRoot : string
}

module DbManager = 

    let dbPath = SettingsManager.configPath DB
    log "db path | %s" dbPath

    let private db = new LiteDatabase(dbPath)
    let private fileCollection = db.GetCollection<QFile>("QFiles")
    let private folderCollection = db.GetCollection<QFolder>("QFolders")
    let private touchCollection = db.GetCollection<QTouch>("QTouchs")

    let updateTouch(item: QTouch) = 
        if item.Id > 0 then
            touchCollection.Update(item) |> ignore
        else
            touchCollection.Insert(item) |> ignore
        (item)

    let queryTouch localPath = 
        let item = touchCollection.Find(fun x -> x.LocalRoot = localPath).FirstOrDefault()
        match obj.ReferenceEquals(null, item) with
        | true -> 
             { QTouch.Id = 0; LocalRoot = localPath; LastTouchTime = DateTime.MinValue }
        | false ->
            item

    let updateFolder (folder: QFolder) = 
        if folder.Id > 0 then
            folderCollection.Update folder |> ignore
            folder
        else
            folderCollection.Insert folder  |> ignore
            folder

    let deleteFolder (id:int) = 
        let bson = LiteDB.BsonValue id
        folderCollection.Delete bson

    let deleteFile (id: int) = 
        let bson = LiteDB.BsonValue id
        fileCollection.Delete bson

    let queryFolder remoteRoot relativePath =
        let fst = folderCollection.Find(fun x -> x.RemoteRoot = remoteRoot && x.RelativePath = relativePath).FirstOrDefault()
        match obj.ReferenceEquals(null, fst) with
        | true ->
            None
        | false ->
            Some fst

    let updateFile(file: QFile) = 
        if file.Id > 0 then
            fileCollection.Update(file) |> ignore
            file
        else
            fileCollection.Insert(file) |> ignore
            file

    let queryFile remoteRoot relativePath = 
        let first = fileCollection.Find(fun x -> x.RemoteRoot = remoteRoot && x.RelativePath = relativePath).FirstOrDefault()
        match obj.ReferenceEquals(null , first) with
        | true -> 
            None
        | false -> 
            Some first