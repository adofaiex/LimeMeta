@echo off
setlocal

cd /d "%~dp0"

if exist .nuget rd .nuget /Q /S
dotnet pack LimeMeta\LimeMeta.csproj --configuration Release --output .nuget
if errorlevel 1 exit /b %errorlevel%

dotnet pack LimeMeta.GraphQL\LimeMeta.GraphQL.csproj --configuration Release --output .nuget
if errorlevel 1 exit /b %errorlevel%

dotnet pack LimeMeta.Templates.csproj --configuration Release --output .nuget
if errorlevel 1 exit /b %errorlevel%

echo.
echo Packages are ready in .nuget
