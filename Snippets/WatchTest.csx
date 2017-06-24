Task("Watch-Test").Does(() => {
    var dir = System.IO.Path.GetDirectoryName(testProject);
    StartProcess("dotnet", new ProcessSettings {
        Arguments = "watch test",
        WorkingDirectory = dir
    });
});
