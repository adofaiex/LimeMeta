@echo off
setlocal

cd /d "%~dp0"

set "PROJECT_NAME=LimeMetaService"
set "WEBAPI_PROJECT=%PROJECT_NAME%.WebAPI\%PROJECT_NAME%.WebAPI.csproj"
set "PUBLISH_DIR=.publish\%PROJECT_NAME%.WebAPI"
set "ZIP_FILE=.publish\%PROJECT_NAME%.WebAPI.zip"

if not exist "%WEBAPI_PROJECT%" (
    echo Cannot find %WEBAPI_PROJECT%.
    exit /b 1
)

echo.
echo [1/5] Restore
dotnet restore
if errorlevel 1 exit /b %errorlevel%

echo.
echo [2/5] Build
dotnet build --configuration Release --no-restore
if errorlevel 1 exit /b %errorlevel%

echo.
echo [3/5] Clean publish directory
if exist "%PUBLISH_DIR%" rd /s /q "%PUBLISH_DIR%"
if not exist ".publish" mkdir ".publish"

echo.
echo [4/5] Publish WebAPI
dotnet publish "%WEBAPI_PROJECT%" --configuration Release --output "%PUBLISH_DIR%" /p:UseAppHost=false --no-restore
if errorlevel 1 exit /b %errorlevel%

echo.
echo [5/5] Zip publish output
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%ZIP_FILE%') { Remove-Item '%ZIP_FILE%' -Force }; Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ZIP_FILE%' -Force"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Publish directory:
echo %CD%\%PUBLISH_DIR%
echo.
echo Zip file:
echo %CD%\%ZIP_FILE%
echo.
