@echo off
setlocal

if exist .nuget rd .nuget /Q /S
dotnet pack LimeMeta\LimeMeta.csproj --configuration Release --output .nuget
if errorlevel 1 exit /b %errorlevel%

dotnet pack LimeMeta.GraphQL\LimeMeta.GraphQL.csproj --configuration Release --output .nuget
