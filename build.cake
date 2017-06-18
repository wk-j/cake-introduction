var solution = "HelloApp.sln";
var mainProject = "Source/HelloApp/HelloApp.csproj";
var libraryProject = "Source/HelloLib/HelloLib.csproj";
var testProject = "Source/HelloLib.Tests/HelloLib.Tests.csproj";

Task("Restore-Solution").Does(() => {
    DotNetCoreRestore(solution);
});

Task("Build-Solution").Does(() => {
    DotNetCoreBuild(solution);
});

Task("Clean-Solution").Does(() => {
    Func<string, IEnumerable<string>> getDirs = (dirName) =>
        System.IO.Directory.EnumerateDirectories("Source", dirName, System.IO.SearchOption.AllDirectories);
    CleanDirectories(getDirs("bin"));
    CleanDirectories(getDirs("obj"));
});

Task("Create-NuGet-Package").Does(() => {
   DotNetCorePack(libraryProject, new DotNetCorePackSettings {
       Configuration = "Release",
       OutputDirectory = "Dist/NuGet"
   });
});

Task("Clean-Dist").Does(() => {
    CleanDirectories("Dist/Release");
    CleanDirectories("Dist/Zip");
});

Task("Build-To-Dist").Does(() => {
    DotNetCoreBuild(mainProject, new DotNetCoreBuildSettings {
        Framework = "netcoreapp1.1",
        Configuration = "Release",
        OutputDirectory = "Dist/Release"
    });
});

Task("Zip-Dist")
    .Does(() => {
        var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(@"Dist/Release/HelloApp.dll");
        var versionNumber = version.FileVersion;
        Zip("Dist/Release", $"Dist/Zip/HelloApp-{versionNumber}.zip");
});

Task("Run-Project").Does(() => {
    DotNetCoreRun(mainProject);
});

Task("Run-Test").Does(() => {
    DotNetCoreTest(testProject);
});

Task("Watch-Run").Does(() => {
    var dir = System.IO.Path.GetDirectoryName(mainProject);
    StartProcess("dotnet", new ProcessSettings {
        Arguments = "watch run",
        WorkingDirectory = dir
    });
});

Task("Watch-Test").Does(() => {
    var dir = System.IO.Path.GetDirectoryName(testProject);
    StartProcess("dotnet", new ProcessSettings {
        Arguments = "watch test",
        WorkingDirectory = dir
    });
});

Task("Publish-NuGet")
    .IsDependentOn("Create-NuGet-Package")
    .Does(() => {
        var npi = EnvironmentVariable("npi");
        var nupkg = new DirectoryInfo("Dist/NuGet").GetFiles("*.nupkg").LastOrDefault();
        var package = nupkg.FullName;
        NuGetPush(package, new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = npi
        });
    });


Task("Hello").Does(() => {
    Console.WriteLine("Hello, world!");
});

var target = Argument("target", "default");
RunTarget(target);