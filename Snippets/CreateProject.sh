dotnet new console  --language C# --output Source/HelloApp
dotnet new classlib --language C# --output Source/HelloLib
dotnet new xunit    --language C# --output Source/HelloLIb.Tests

dotnet new sln --name HelloApp --output .

dotnet sln HelloApp.sln add Source/HelloApp/HelloApp.csproj
dotnet sln HelloApp.sln add Source/HelloLib/HelloLib.csproj
dotnet sln HelloApp.sln add Source/HelloLib.Tests/HelloLib.Tests.csproj

dotnet add Source/HelloApp/HelloApp.csproj reference Source/HelloLib/HelloLib.csproj
dotnet add Source/HelloLib.Tests/HelloLib.Tests.csproj reference Source/HelloLib/HelloLib.csproj