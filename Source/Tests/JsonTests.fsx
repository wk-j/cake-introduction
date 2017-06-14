

#r "../../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"

open Newtonsoft.Json


JsonConvert.SerializeObject(System.DateTime.Now) |> printfn "%A"