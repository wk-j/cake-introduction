module EasySyncClient.Utility

open System

let log (f: 'a Printf.TextWriterFormat) = 
    printf "[%s] " (DateTime.Now.ToString())
    printfn f