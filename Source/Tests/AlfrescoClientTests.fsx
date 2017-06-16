
#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient.Client

let client = AlfrescoClient( { User = "admin"; Password = "admin"; Url = "http://192.168.0.109:8080"})

client.TryCreateDirectories "/alfresco/webdav/Validate"  "SecA/SecB/SecC" |> printfn "%A"

