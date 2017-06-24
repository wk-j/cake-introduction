Task("Run-Project").Does(() => {
    DotNetCoreRun(mainProject);
});