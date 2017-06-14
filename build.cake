var project = "Source/EasySyncClient/EasySyncClient.fsproj";

Task("Build").Does(() => {
    MSBuild(project);
});

var target = Argument("target", "default");
RunTarget(target);