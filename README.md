## Cake Introduction

## 1. Install bootstraper

### Windows

```
Invoke-WebRequest http://cakebuild.net/download/bootstrapper/windows -OutFile build.ps1
```

### Linux 

```
curl -Lsfo build.sh http://cakebuild.net/download/bootstrapper/linux
```

### mac OS

```
curl -Lsfo build.sh http://cakebuild.net/download/bootstrapper/osx
```

## 2. Say Hello

#### Create build.cake

```csharp
Task("Default").Does(() => {
    Information("Hello, world!");
});

var target = Argument("target", "Default");
RunTarget(target);
```

#### Windows

```
& build.ps1 
```

#### Linux / mac OS

```
./build.sh 
```


## 3. Create Projects

```bash
dotnet new console  --language C# --output Source/HelloApp
dotnet new classlib --language C# --output Source/HelloLib
dotnet new xunit    --language C# --output Source/HelloLIb.Tests

dotnet new sln --name HelloApp --output .

dotnet sln HelloApp.sln add Source/HelloApp/HelloApp.csproj
dotnet sln HelloApp.sln add Source/HelloLib/HelloLib.csproj
dotnet sln HelloApp.sln add Source/HelloLib.Tests/HelloLib.Tests.csproj

dotnet add Source/HelloApp/HelloApp.csproj reference Source/HelloLib/HelloLib.csproj
dotnet add Source/HelloLib.Tests/HelloLib.Tests.csproj reference Source/HelloLib/HelloLib.csproj
```

## 4. Restore Packages

### build.cake

```csharp
var solution = "HelloApp.sln";

Task("Restore-Solution").Does(() => {
    DotNetCoreRestore(solution);
});

var target = Argument("target", "Hello");
RunTarget(target);
```

### Run

```
& build.ps1 -target Restore-Solution
./build.sh  --target Restore-Solution
```

## 4. Build 

### build.cake

```csharp
Task("Build-Solution").Does(() => {
    DotNetCoreBuild(solution);
});
```

### Run

```
& build.ps1 -target Build-Solution
./build.sh --target Build-Solution
```

## 5. Run

### build.cake

```csharp
var mainProject = "Source/HelloApp/HelloApp.csproj";
Task("Run-Project").Does(() => {
    DotNetCoreRun(mainProject);
});
```

### Run

```
& build.ps1 -target Run-Project
./build.sh --target Run-Project
```

## 6. Test

### build.cake

```csharp
var testProject = "Source/HelloLib.Tests/HelloLib.Tests.csproj";
Task("Test-Project").Does(() => {
    DotNetCoreTest(testProject);
});
```

### Run

```
& buikld.ps1 -target Test-Project
./build.sh --target Test-Project
```

## 7. Watch Test

### Add watcher tools into Source/HelloLib.Tests/HelloLib.Tests.csproj

```xml
 <ItemGroup>
     <DotNetCliToolReference Include="Microsoft.DotNet.Watcher.Tools" Version="1.0.0" />
  </ItemGroup>
```

### Start process

```csharp
Task("Watch-Test").Does(() => {
    StartProcess("dotnet", new ProcessSettings {
        Arguments = "watch test",
        WorkingDirectory = "Source/HelloLib.Tests"
    });
});
```

### Run

```
& build.ps1 -target Watch-Test
./build.sh --target Watch-Test
```

## 8. Watch Run

```csharp
Task("Watch-Run").Does(() => {
    StartProcess("dotnet", new ProcessSettings {
        Arguments = "watch run",
        WorkingDirectory = "Source/HelloApp"
    });
});
```

### Run

```
& build.ps1 -target Watch-Test
./build.sh --target Watch-Test
```

## 9. Create NuGet package

### Add NuGet property into Source/HelloLib/HelloLib.csproj

```xml
   <Version>0.1.0.0</Version>
   <Authors>wk</Authors>
   <Title>HelloLib</Title>
   <Description>Just Say Hello</Description>
   <ProjectUrl>https://github.com/wk-j/cake-introduction</ProjectUrl>
```

### build.cake

```csharp
Task("Create-NuGet-Package").Does(() => {
   DotNetCorePack(libraryProject, new DotNetCorePackSettings {
       Configuration = "Release",
       OutputDirectory = "Dist/NuGet"
   });
});
```

### Run

```
& build.ps1 -target Create-NuGet-Package
./build.sh --target Create-NuGet-Package
```

## 10. Publish NuGet package

### build.cake

```csharp
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
```

### Run

```
& build.ps1 -target Publish-NuGet
./build.sh --target Publish-NuGet
```

## 11. Create GitHub release

### build.cake

```csharp
#tool "nuget:?package=gitreleasemanager"

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
```

### Run

```
& build.ps1 -target Create-GitHub-Release
./build.sh --target Create-GitHub-Release
```