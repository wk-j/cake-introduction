var project = "Source/EasySyncClient/EasySyncClient.fsproj";
var console = "Source/EasySyncClient.Console/EasySyncClient.Console.fsproj";

Task("Build").Does(() => {
    MSBuild(console);
});

Task("Watch").Does(() => {
    // watchman-make -p '**/*.fs' --make='./build.sh --target' -t Build
    StartProcess("watchman-make", new ProcessSettings {
        Arguments = "-p '**/*.fs' --make='./build.sh --target' -t Build"
    });
});

var target = Argument("target", "default");
RunTarget(target);