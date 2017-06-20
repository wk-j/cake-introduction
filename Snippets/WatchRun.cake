
Task("Watch-Run").Does(() => {
    var dir = System.IO.Path.GetDirectoryName(mainProject);
    StartProcess("dotnet", new ProcessSettings {
        Arguments = "watch run",
        WorkingDirectory = dir
    });
});
