Task("Build-Solution").Does(() => {
    BuildDotNetCore(solution);
});