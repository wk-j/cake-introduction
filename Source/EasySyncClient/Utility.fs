module EasySyncClient.Utility

open System
open System.IO
open System.Security.Cryptography

let log (f: 'a Printf.TextWriterFormat) = 
    printf "[%s] " (DateTime.Now.ToString())
    printfn f

let checkMd5 localFile = 
    use md5 = MD5.Create()
    use stream = File.OpenRead(localFile)
    let by = md5.ComputeHash(stream)
    BitConverter.ToString(by).Replace("-","‌​").ToLower()
