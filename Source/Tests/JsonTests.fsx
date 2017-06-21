#r "../../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"

open Newtonsoft.Json
open System.IO


JsonConvert.SerializeObject(System.DateTime.Now) |> printfn "%A"

let path = "Source/Tests/JsonTests.fsx"
Path.GetDirectoryName path |> printfn "%A"

Path.GetDirectoryName "Hello.fsx" |> printfn "%A"