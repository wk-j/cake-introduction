Task("Test-Project").Does(() => {
    DotNetCoreTest(testProject);
});