
Task("Restore-Solution").Does(() => {
    DotNetCoreRestore(solution);
});