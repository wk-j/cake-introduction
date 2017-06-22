//#r "../../packages/Apache.DotCMIS/lib/DotCMIS.dll"
#r "../../packages/OpenDataSpace.DotCMIS/lib/net40/DotCMIS.dll"

open System.Collections.Generic

open System
open DotCMIS
open DotCMIS.Client.Impl
open System.Linq

let start() = 
    let parameters =  Dictionary<string, string>();

    parameters.[SessionParameter.BindingType] <- BindingType.AtomPub
   // parameters.[SessionParameter.AtomPubUrl] <- "http://192.168.0.109:8080/alfresco/api/-default-/public/cmis/versions/1.1/atom"
    parameters.[SessionParameter.AtomPubUrl] <- "http://192.168.0.109:8080/alfresco/api/-default-/public/cmis/versions/1.1/atom"
    parameters.[SessionParameter.User] <- "admin"
    parameters.[SessionParameter.Password] <- "admin"
    parameters.[SessionParameter.RepositoryId] <- "-default-"
    //parameters.[SessionParameter.MaximumRequestRetries] <- "10000"

    let factory = SessionFactory.NewInstance()
    let session = factory.CreateSession(parameters)
    
    session.RepositoryInfo.VendorName |> printfn "%A"

    let rootFolder = session.GetRootFolder();

    printfn "%A" rootFolder.Name

    let results = session.Query("SELECT cmis:name, cmis:objectId FROM cmis:document WHERE cmis:name LIKE 'Test1.txt'", false).GetPage(1)

    for r in results do

        let id = r.GetPropertyById("cmis:objectId").Values |> Seq.head |> sprintf "%A"

        printfn "%A" <| r.GetPropertyById("cmis:name").FirstValue
        printfn "%A" <| r.GetPropertyById("cmis:objectId").Values

        let str = id.ToString()
        printfn "%A" str
        let obj = session.GetObject(str)
        obj.Name |> printfn "%A"

start()