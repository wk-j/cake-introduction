Task("Build-Solution").Does(() => {
    DotNetCoreBuild(solution);
});