
module EasySyncClient.CmisClient

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open System.Collections.Generic
open EasySyncClient.Models
open EasySyncClient.ClientModels
open System

type SyncObject =
    | File of FullPath * RelativePath
    | Folder of FullPath * RelativePath
and FullPath = FullPath of string
and RelativePath = RelativePath of string

let private createSession () = 
    let parameters =  Dictionary<string, string>();

    parameters.[SessionParameter.BindingType] <- BindingType.AtomPub
    parameters.[SessionParameter.AtomPubUrl] <- "http://192.168.0.109:8080/alfresco/api/-default-/public/cmis/versions/1.1/atom"
    parameters.[SessionParameter.User] <- "admin"
    parameters.[SessionParameter.Password] <- "admin"
    parameters.[SessionParameter.RepositoryId] <- "-default-"

    let factory = SessionFactory.NewInstance()
    factory.CreateSession(parameters)

type CmisClient (remoteRoot, localRoot) = 
    let meetObject = new Event<SyncObject>()
    let onMeetObject = meetObject.Publish

    let rec syncChild (folder: IFolder) = 
        let childs = folder.GetChildren()
        for c in childs do
            if c :? DotCMIS.Client.Impl.Folder then
                let f = c :?> IFolder    
                let path = f.Path
                let full =  FullPath path
                let relative = RelativePath <| path.Replace(remoteRoot, "")
                meetObject.Trigger <| Folder (full, relative)
                syncChild f
            else if c :? DotCMIS.Client.Impl.Document then
                let d = c :?> IDocument
                let path = String.Join("/", d.Paths)
                let full = FullPath path
                let relative = RelativePath <| path.Replace(remoteRoot, "")
                meetObject.Trigger <| File (full, relative)

    member this.OnMeetObject = onMeetObject

    member this.StartSync() = 
        let session = createSession()
        let folder = session.GetObjectByPath(remoteRoot) :?> IFolder
        syncChild folder