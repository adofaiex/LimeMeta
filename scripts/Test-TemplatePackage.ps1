[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory,
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
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

    & dotnet new limemeta -n GeneratedService -o $projectRoot --debug:custom-hive $hiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw "无法生成模板项目。"
    }

    $expectedProjects = @(
        "LimeMeta/LimeMeta.csproj",
        "LimeMeta.GraphQL/LimeMeta.GraphQL.csproj",
        "GeneratedService/GeneratedService.csproj",
        "GeneratedService.WebAPI/GeneratedService.WebAPI.csproj"
    )
    foreach ($relativePath in $expectedProjects) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $relativePath))) {
            throw "生成项目缺少：$relativePath"
        }
    }

    $generatedProjects = @(Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter "*.csproj")
    if ($generatedProjects.Count -ne 4) {
        throw "模板应生成四个项目，实际为 $($generatedProjects.Count) 个。"
    }

    $allProjectText = $generatedProjects |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw } |
        Join-String -Separator "`n"
    if ($allProjectText -match 'PackageReference\s+Include="LimeMeta(?:\.GraphQL)?"') {
        throw "生成项目仍包含 LimeMeta 框架包引用。"
    }

    $businessProject = Get-Content -LiteralPath (
        Join-Path $projectRoot "GeneratedService/GeneratedService.csproj") -Raw
    if ($businessProject -notmatch 'ProjectReference\s+Include="\.\.\\LimeMeta\\LimeMeta\.csproj"' -or
        $businessProject -notmatch 'ProjectReference\s+Include="\.\.\\LimeMeta\.GraphQL\\LimeMeta\.GraphQL\.csproj"') {
        throw "业务项目没有通过 ProjectReference 引用两个框架源码项目。"
    }

    foreach ($relativePath in @(
        "LimeMeta/Extensions.cs",
        "LimeMeta/Data/FreeSqlLimeMeta.cs",
        "LimeMeta.GraphQL/QueryType.cs",
        "LICENSE",
        "NOTICE"
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $relativePath))) {
            throw "生成结果缺少框架源码或许可证文件：$relativePath"
        }
    }

    if (Get-ChildItem -LiteralPath $projectRoot -Recurse -Force |
        Where-Object { $_.FullName -match '[\\/](PublicAPI\.(?:Shipped|Unshipped)\.txt|bin|obj)([\\/]|$)' }) {
        throw "生成结果包含 PublicAPI 基线或构建产物。"
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
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $relativePath))) {
            throw "生成项目缺少开发文档：$relativePath"
        }
    }

    $generatedReadme = Get-Content -LiteralPath (Join-Path $projectRoot "README.md") -Raw
    if ($generatedReadme -notmatch "GeneratedService 开发指南" -or
        $generatedReadme -match "LimeMetaService 开发指南") {
        throw "模板 README 没有正确替换项目名称。"
    }

    $generatedSolution = Get-ChildItem -LiteralPath $projectRoot -Filter "*.sln" |
        Select-Object -First 1
    if (-not $generatedSolution) {
        throw "模板没有生成解决方案文件。"
    }
    $solutionProjects = & dotnet sln $generatedSolution.FullName list
    if ($LASTEXITCODE -ne 0 -or @($solutionProjects | Where-Object { $_ -match '\.csproj$' }).Count -ne 4) {
        throw "生成解决方案没有包含四个项目。"
    }

    & dotnet restore $generatedSolution.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "生成项目还原失败。"
    }
    & dotnet build $generatedSolution.FullName -c Release --no-restore -warnaserror
    if ($LASTEXITCODE -ne 0) {
        throw "生成项目构建失败。"
    }

    $sourceSmokePath = Join-Path $projectRoot "LimeMeta/TemplateSourceSmoke.cs"
    [System.IO.File]::WriteAllText(
        $sourceSmokePath,
        "namespace LimeMeta; internal static class TemplateSourceSmoke { internal const bool Enabled = true; }",
        [System.Text.UTF8Encoding]::new($false))
    & dotnet build $generatedSolution.FullName -c Release --no-restore -warnaserror
    if ($LASTEXITCODE -ne 0) {
        throw "修改生成后的框架源码后重新构建失败。"
    }
}
finally {
    Remove-Item -LiteralPath $smokeRoot -Recurse -Force
}

Write-Host "模板本地安装、四项目源码生成、还原和构建检查通过。"
