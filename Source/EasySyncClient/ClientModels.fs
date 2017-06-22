module EasySyncClient.ClientModels

open System.IO
open System
open System.Linq

type RemoteRelativePath = RemoteRelativePath of string
type RemoteRelativeNoName = RemoteRelativeNoName of string
type LocalRelativePath = LocalRelativePath of string

type FullRemotePath = FullRemotePath of string
type FullRemotePathNoName = FullRemotePathNoName of string

type FullLocalPath = FullLocalPath of string

type RemoteSection = RemoteSection of string array
type RemoteRoot = RemoteRoot of string
type LocalRoot = LocalRoot of string

type Action  = 
    | CreateDir of RemoteRoot * RemoteRelativePath
    | CreateFile of RemoteRelativePath * LocalRelativePath

type MoveInfo = {
    OldPath : string
    NewPath : string
}

let splitWith (sep: string) (str: string) = str.Split([| sep |], StringSplitOptions.None)
let cleanPath (str: string) = str.TrimEnd('/').Replace("//", "/").Replace("\\", "/")
let toSections (data: string array) = [ for i in 0..data.Length do yield String.concat "/"  <| data.Take(i) ]
let replace (a:string) b (c: string)= c.Replace(a, b)

let createRelativeRemotePath (LocalRoot localRoot) (FullLocalPath localPath) = 
    let relative = replace localRoot "" localPath 
    let path = Path.GetDirectoryName relative
    let name = Path.GetFileName relative 
    relative |> RemoteRelativePath

let createRemoteSection (RemoteRelativeNoName path) =
    let path = path |> cleanPath
    splitWith "/" path |> toSections |> List.map RemoteRelativePath

let createFullRemotePath (RemoteRoot root) (RemoteRelativePath path)  = 
    root + "/" + path |> cleanPath  |> FullRemotePath

let createFullRemotePathNoName (RemoteRoot root) (RemoteRelativeNoName path) = 
    root + "/" + path |> cleanPath  |> FullRemotePathNoName

let extractFullRemotePath (FullRemotePath path) = 
    let last = path |> splitWith "/" |> Array.last
    let path = Path.GetDirectoryName path
    (path, last)
