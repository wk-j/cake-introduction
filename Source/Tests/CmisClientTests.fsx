#r "../../Source/EasySyncClient/bin/Debug/EasySyncClient.dll"

open EasySyncClient

let go() =
    CmisClient.syncRemoteFolder "/E-Tax" "Resource"

go()
