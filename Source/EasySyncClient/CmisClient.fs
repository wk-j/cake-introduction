
module EasySyncClient.CmisClient

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open System.Collections.Generic

open EasySyncClient.Models
open EasySyncClient.ClientModels
open EasySyncClient.Utility

let private createSession () = 
    let parameters =  Dictionary<string, string>();

    parameters.[SessionParameter.BindingType] <- BindingType.AtomPub
    parameters.[SessionParameter.AtomPubUrl] <- "http://192.168.0.109:8080/alfresco/api/-default-/public/cmis/versions/1.1/atom"
    parameters.[SessionParameter.User] <- "admin"
    parameters.[SessionParameter.Password] <- "admin"
    parameters.[SessionParameter.RepositoryId] <- "-default-"

    let factory = SessionFactory.NewInstance()
    factory.CreateSession(parameters)

let rec private syncChild (folder: IFolder) = 
    log "folder %s" folder.Path
    let childs = folder.GetChildren()
    for c in childs do
        if c :? DotCMIS.Client.Impl.Folder then
            let f = c :?> IFolder    
            syncChild f

let syncRemoteFolder remoteRoot localRoot = 
    let session = createSession()
    let folder = session.GetObjectByPath(remoteRoot) :?> IFolder
    syncChild folder