module EasySyncClient.DB

open System
open LiteDB
open EasySyncClient.Utility
open System.Linq

type FileAction = 
    | Created = 0
    | Moved = 1
    | Renamed = 2
    | Deleted = 3
    | Changed = 4

type Status = 
    | Initialize = 0
    | ProcessSuccess = 1
    | ProcessFailed = 2

[<CLIMutable>]
type QFile = {
    Id : int
    LocalPath : string
    //FileAction: FileAction
    //Status: Status
    //OriginalPath : string
    //NewPath : string
    //CreationTime: DateTime
    //LastWriteTime: DateTime
    //LastAccessTime: DateTime
    //RemoteRoot: string
    //LocalRoot: string
    Md5 : string
}

[<CLIMutable>]
type QTouch = {
    Id : int
    LastTouchTime : DateTime
    LocalPath : string
}

module DbManager = 

    let private db = new LiteDatabase("Data/Files.db")
    let private fileCollection = db.GetCollection<QFile>("QFiles")
    let private touchCollection = db.GetCollection<QTouch>("QTouchs")

    let updateTouch(item: QTouch) = 
        if item.Id > 0 then
            touchCollection.Update(item) |> ignore
        else
            touchCollection.Insert(item) |> ignore
        (item)

    let queryTouch localPath = 
        let item = touchCollection.Find(fun x -> x.LocalPath = localPath).FirstOrDefault()
        match obj.ReferenceEquals(null, item) with
        | true -> 
             { QTouch.Id = 0; LocalPath = localPath; LastTouchTime = DateTime.MinValue }
        | false ->
            item

    let updateFile(file:QFile) = 
        if file.Id > 0 then
            fileCollection.Update(file) |> ignore
            file
        else
            fileCollection.Insert(file) |> ignore
            file

    let queryFile localPath = 
        let fst = fileCollection.Find(fun x -> x.LocalPath = localPath).FirstOrDefault()
        match obj.ReferenceEquals(null, fst) with
        | true ->
            let file = { Id  = 0 ; LocalPath = localPath; Md5 =  "" }
            updateFile file
        | false ->
            fst