if exist .nuget rd .nuget /Q /S
dotnet pack LimeMeta.sln --output .nuget

rem Publish manually with your own NuGet source and API key.
rem Example:
rem dotnet nuget push .nuget\*.nupkg --source <source-name-or-url> --skip-duplicate --api-key <api-key>
