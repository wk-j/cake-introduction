module EasySyncClient.DB

open System
open LiteDB

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
    Action: FileAction
    Status: Status
    OriginalPath : string
    OriginalName : string
    DateTime: DateTime
}

module DbManager = 

    let private db = new LiteDatabase("Data/Files.db")
    let private fileCollection = db.GetCollection<QFile>("QFiles")

    let updateFile(file:QFile) = 
        if file.Id > 0 then
            fileCollection.Update(file) |> ignore
            file
        else
            fileCollection.Insert(file) |> ignore
            file

    let queryFile status  =
        fileCollection.Find(fun x -> x.Status = status)