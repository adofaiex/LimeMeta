[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory,
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$templatePackage = Join-Path $packageRoot "LimeMeta.Templates.$Version.nupkg"
if (-not (Test-Path -LiteralPath $templatePackage)) {
    throw "缺少模板包：$templatePackage"
}

$smokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("limemeta-template-" + [guid]::NewGuid().ToString("N"))
$hiveRoot = Join-Path $smokeRoot "hive"
$projectRoot = Join-Path $smokeRoot "GeneratedService"
New-Item -ItemType Directory -Path $hiveRoot, $projectRoot | Out-Null

try {
    & dotnet new install $templatePackage --force --debug:custom-hive $hiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw "无法从本地 nupkg 安装模板。"
    }

    & dotnet new limemeta -n GeneratedService -o $projectRoot --limeMetaVersion $Version --debug:custom-hive $hiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw "无法生成模板项目。"
    }

    $generatedProjects = @(Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter "*.csproj")
    if ($generatedProjects.Count -eq 0) {
        throw "模板没有生成项目文件。"
    }

    $frameworkProject = $generatedProjects |
        Where-Object {
            (Get-Content -LiteralPath $_.FullName -Raw) -match
                'Include="LimeMeta\.GraphQL"\s+Version="' + [regex]::Escape($Version) + '"'
        } |
        Select-Object -First 1
    if (-not $frameworkProject) {
        throw "生成项目没有精确引用 LimeMeta.GraphQL $Version。"
    }

    $developmentConfig = Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter "appsettings.Development.yml" |
        Select-Object -First 1
    if (-not $developmentConfig -or
        (Get-Content -LiteralPath $developmentConfig.FullName -Raw) -notmatch "DataType:\s*['`"]?MySql") {
        throw "生成项目默认数据库不是 MySQL。"
    }

    $expectedDocuments = @(
        "README.md",
        "docs/01-overview.md",
        "docs/02-models-and-graphql.md",
        "docs/03-users-and-authorization.md",
        "docs/04-logic-and-extensions.md",
        "docs/05-configuration-and-deployment.md"
    )
    foreach ($relativePath in $expectedDocuments) {
        $documentPath = Join-Path $projectRoot $relativePath
        if (-not (Test-Path -LiteralPath $documentPath)) {
            throw "生成项目缺少开发文档：$relativePath"
        }
    }

    $generatedReadme = Get-Content -LiteralPath (Join-Path $projectRoot "README.md") -Raw
    if ($generatedReadme -notmatch "GeneratedService 开发指南" -or
        $generatedReadme -match "LimeMetaService 开发指南") {
        throw "模板 README 没有正确替换项目名称。"
    }

    $nugetConfigPath = Join-Path $smokeRoot "NuGet.config"
    $escapedPackageRoot = [System.Security.SecurityElement]::Escape($packageRoot)
    $config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$escapedPackageRoot" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
    [System.IO.File]::WriteAllText($nugetConfigPath, $config, [System.Text.UTF8Encoding]::new($false))

    $generatedSolution = Get-ChildItem -LiteralPath $projectRoot -Filter "*.sln" |
        Select-Object -First 1
    if (-not $generatedSolution) {
        throw "模板没有生成解决方案文件。"
    }

    & dotnet restore $generatedSolution.FullName --configfile $nugetConfigPath
    if ($LASTEXITCODE -ne 0) {
        throw "生成项目还原失败。"
    }
    & dotnet build $generatedSolution.FullName -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "生成项目构建失败。"
    }
}
finally {
    Remove-Item -LiteralPath $smokeRoot -Recurse -Force
}

Write-Host "模板本地安装、生成、还原和构建检查通过。"
