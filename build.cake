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

Task("Run").Does(() => {
    StartProcess("mono", new ProcessSettings {
        Arguments = "Source/EasySyncClient.Console/bin/Debug/EasySyncClient.Console.exe"
    });
});

Task("Create-Zip")
    .IsDependentOn("Build")
    .Does(() => {
        //var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(@"Source/EasySyncClient.Console/bin/Debug/EasySyncClient.Console.exe").FileVersion;
        //var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(@"Source/EasySyncClient.Console/bin/Debug/EasySyncClient.Console.exe").FileVersionx;
        var version = "0.0.1";
        Zip("Source/EasySyncClient.Console/bin/Debug", $"Release/Zip/EasySyncClient.Console-{version}.zip");
});


var target = Argument("target", "default");
RunTarget(target);