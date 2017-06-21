#r "../../packages/LiteDB/lib/net35/LiteDB.dll"
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.DB
open System

let file = 
    { QFile.Id = 0 
      Action = FileAction.Changed
      Status = Status.Initialize
      OriginalPath = "Resource/Test1.txt"
      OriginalName = "Test1.txt"
      DateTime = DateTime.Now }

let query() =
    DbManager.queryFile Status.Initialize |> printfn "%A"

let update() =
    let q = DbManager.queryFile Status.Initialize |> Seq.toList
    if q.Length > 0 then
        let q = q.Head
        let n = { q with Status = Status.ProcessSuccess }
        DbManager.updateFile n |> ignore
    
let start() =
    //insert()
    update()
    query()