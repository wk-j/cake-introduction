#tool "nuget:?package=gitreleasemanager"

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

Task("Run-Project").Does(() => {
    DotNetCoreRun(mainProject);
});


Task("Clean-Solution").Does(() => {
    Func<string, IEnumerable<string>> getDirs = (dirName) =>
        System.IO.Directory.EnumerateDirectories("Source", dirName, System.IO.SearchOption.AllDirectories);
    CleanDirectories(getDirs("bin"));
    CleanDirectories(getDirs("obj"));
});


Task("Clean-Dist").Does(() => {
    CleanDirectories("Dist/Release");
    CleanDirectories("Dist/Zip");
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

Task("Create-NuGet-Package").Does(() => {
   DotNetCorePack(libraryProject, new DotNetCorePackSettings {
       Configuration = "Release",
       OutputDirectory = "Dist/NuGet"
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

Task("Build-Release").Does(() => {
    DotNetCoreBuild(mainProject, new DotNetCoreBuildSettings {
        Framework = "netcoreapp1.1",
        Configuration = "Release",
        OutputDirectory = "Dist/Release"
    });
});

Task("Create-Zip")
    .Does(() => {
        var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(@"Dist/Release/HelloApp.dll").FileVersion;
        Zip("Dist/Release", $"Dist/Zip/HelloApp-{version}.zip");
});

Task("Create-Github-Release")
    .IsDependentOn("Build-Release")
    .IsDependentOn("Create-Zip")
    .Does(() => {
        var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(@"Dist/Release/HelloApp.dll").FileVersion;
        var appName = "HelloApp";

        var tag = $"v{version}";
        var args = $"tag -a {tag} -m {appName}-{tag}";
        var zip = $"Dist/Zip/{appName}-{version}.zip";

        var user = EnvironmentVariable("ghu");      // Github's user
        var token = EnvironmentVariable("ghp");     // Github's token

        var owner = "wk-j";
        var repo = "cake-introduction";

        StartProcess("git", new ProcessSettings {
            Arguments = args
        });

        StartProcess("git", new ProcessSettings {
            Arguments = $"push https://{user}:{token}@github.com/{owner}/{repo}.git {tag}"
        });

        GitReleaseManagerCreate(user, token, owner , repo, new GitReleaseManagerCreateSettings {
            Name              = tag,
            InputFilePath     = "Release/RELEASE.md",
            Prerelease        = false,
            TargetCommitish   = "master",
        });
        GitReleaseManagerAddAssets(user, token, owner, repo, tag, zip);
        GitReleaseManagerPublish(user, token, owner , repo, tag);
});

Task("Default").Does(() => {
    Information("Hello, world!");
});

var target = Argument("target", "Default");
RunTarget(target);