#r "../../packages/LiteDB/lib/net35/LiteDB.dll"
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.DB
open System

let file = 
    { QFile.Id = 0 
      FileAction = FileAction.Changed
      Status = Status.Initialize
      OriginalPath = "Resource/Test1.txt"
      CreationTime = DateTime.Now
      LastWriteTime = DateTime.Now
      NewPath = ""
      RemoteRoot = ""
      LocalRoot = ""
      LastAccessTime = DateTime.Now }

let query() =
    let data = DbManager.queryFile Status.Initialize DateTime.MinValue |> Seq.toList
    data |> printfn "%A"

let update() =
    let q = DbManager.queryFile Status.Initialize  DateTime.MinValue |> Seq.toList
    if q.Length > 0 then
        let q = q.Head
        let n = { q with Status = Status.ProcessSuccess }
        DbManager.updateFile n |> ignore
    
let start() =
    //insert()
    //update()
    query()

start()
