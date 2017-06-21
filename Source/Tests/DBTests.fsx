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

let insert() =
    insertFile file

let query() =
    queryFile Status.Initialize |> printfn "%A"

//insert()
//query()

