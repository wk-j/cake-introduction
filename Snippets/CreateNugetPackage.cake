Task("Create-NuGet-Package").Does(() => {
   DotNetCorePack(libraryProject, new DotNetCorePackSettings {
       Configuration = "Release",
       OutputDirectory = "Dist/NuGet"
   });
});