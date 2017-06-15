var solution = "HelloApp.sln";
var mainProject = "Source/HelloApp/HelloApp.csproj";
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

Task("Clean-Solution").Does(() => {
    Func<string, IEnumerable<string>> getDirs = (dirName) =>
        System.IO.Directory.EnumerateDirectories("Source", dirName, System.IO.SearchOption.AllDirectories);
    CleanDirectories(getDirs("bin"));
    CleanDirectories(getDirs("obj"));
});

Task("Pack-Project").Does(() => {
    DotNetCorePack(mainProject);
});

Task("Hello").Does(() => {
    Console.WriteLine("Hello, world!");
});

var target = Argument("target", "default");
RunTarget(target);