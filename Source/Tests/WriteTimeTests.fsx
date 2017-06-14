
open System.IO
open System.Linq

let files = Directory.EnumerateFiles("/Users/wk/Source/project/easy-sync/EasySyncClient/Resource", "*").Select(FileInfo).Select(fun x -> x.LastWriteTime, x.Name)
printfn "%A" files