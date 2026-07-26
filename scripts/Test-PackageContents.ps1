[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory,
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$templateFileName = "LimeMeta.Templates.$Version.nupkg"
$templatePackage = Join-Path $packageRoot $templateFileName
if (-not (Test-Path -LiteralPath $templatePackage)) {
    throw "缺少模板包：$templateFileName"
}

$packages = @(Get-ChildItem -LiteralPath $packageRoot -Filter "*.nupkg" -File)
if ($packages.Count -ne 1 -or $packages[0].Name -ne $templateFileName) {
    throw "发布目录只能包含一个 LimeMeta.Templates nupkg。"
}
if (Get-ChildItem -LiteralPath $packageRoot -Filter "*.snupkg" -File) {
    throw "模板发布不应包含符号包。"
}

$forbiddenPatterns = @(
    "ghp_[A-Za-z0-9]+",
    "github_pat_[A-Za-z0-9_]+",
    "nuget\.pkg\.github\.com/adofaiex",
    "<packageSourceCredentials",
    "ClearTextPassword",
    "__LIMEMETA_VERSION__",
    "[A-Z]:\\Users\\",
    "/home/[^/]+/"
)

$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("limemeta-audit-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $extractRoot | Out-Null
try {
    Expand-Archive -LiteralPath $templatePackage -DestinationPath $extractRoot
    $entries = @(Get-ChildItem -LiteralPath $extractRoot -Recurse -File -Force)
    $relativeEntries = @($entries | ForEach-Object {
        [System.IO.Path]::GetRelativePath($extractRoot, $_.FullName).Replace("\", "/")
    })

    foreach ($required in @(
        "LICENSE",
        "NOTICE",
        "limemeta-icon.png",
        "content/templates/LimeMeta.Service/LICENSE",
        "content/templates/LimeMeta.Service/NOTICE",
        "content/templates/LimeMeta.Service/LimeMeta/LimeMeta.csproj",
        "content/templates/LimeMeta.Service/LimeMeta/Extensions.cs",
        "content/templates/LimeMeta.Service/LimeMeta.GraphQL/LimeMeta.GraphQL.csproj",
        "content/templates/LimeMeta.Service/LimeMeta.GraphQL/QueryType.cs"
    )) {
        if ($relativeEntries -notcontains $required) {
            throw "$templateFileName 缺少 $required"
        }
    }

    if ($relativeEntries | Where-Object {
        $_ -match "(^|/)(bin|obj)/" -or
        $_ -match "PublicAPI\.(Shipped|Unshipped)\.txt$" -or
        $_ -match "content/templates/LimeMeta\.Service/LimeMeta(\.GraphQL)?/README\.md$"
    }) {
        throw "$templateFileName 含有构建产物、PublicAPI 基线或框架包 README。"
    }

    foreach ($entry in $entries) {
        if ($entry.Length -gt 10MB) {
            continue
        }

        $bytes = [System.IO.File]::ReadAllBytes($entry.FullName)
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        foreach ($pattern in $forbiddenPatterns) {
            if ($text -match $pattern) {
                throw "$templateFileName 的 $($entry.Name) 命中禁止内容：$pattern"
            }
        }
    }

    $nuspec = Get-ChildItem -LiteralPath $extractRoot -Filter "*.nuspec" | Select-Object -First 1
    if (-not $nuspec) {
        throw "$templateFileName 缺少 nuspec。"
    }
    [xml]$metadataDocument = Get-Content -LiteralPath $nuspec.FullName -Raw
    $metadata = $metadataDocument.package.metadata
    if ([string]$metadata.license.type -ne "expression" -or
        [string]$metadata.license.InnerText -ne "Apache-2.0") {
        throw "$templateFileName 的许可证表达式应为 Apache-2.0。"
    }
    if ($metadata.icon -ne "limemeta-icon.png" -or [string]::IsNullOrWhiteSpace($metadata.readme)) {
        throw "$templateFileName 缺少图标或 README 元数据。"
    }
    if ($metadata.repository.type -ne "git" -or
        $metadata.repository.url -ne "https://github.com/adofaiex/LimeMeta" -or
        [string]::IsNullOrWhiteSpace($metadata.repository.commit)) {
        throw "$templateFileName 缺少正确的 Git 仓库与提交元数据。"
    }
    if ($metadata.packageTypes.packageType.name -ne "Template") {
        throw "模板包缺少 Template 包类型。"
    }

    $templateJson = Get-ChildItem -LiteralPath $extractRoot -Recurse -Filter "template.json" -Force |
        Select-Object -First 1
    if (-not $templateJson) {
        throw "模板包缺少 template.json。"
    }
    $templateText = Get-Content -LiteralPath $templateJson.FullName -Raw
    if ($templateText -match "limeMetaVersion|__LIMEMETA_VERSION__") {
        throw "模板仍包含已废弃的框架包版本参数。"
    }

    $frameworkProjectTexts = @(
        Get-Content -LiteralPath (
            Join-Path $extractRoot "content/templates/LimeMeta.Service/LimeMeta/LimeMeta.csproj") -Raw
        Get-Content -LiteralPath (
            Join-Path $extractRoot "content/templates/LimeMeta.Service/LimeMeta.GraphQL/LimeMeta.GraphQL.csproj") -Raw
    )
    if (($frameworkProjectTexts -join "`n") -match
        "PackageId|PackageReadmeFile|Microsoft\.SourceLink|PublicApiAnalyzers|GenerateNuspec") {
        throw "内置框架项目仍包含 NuGet 打包或公共包 API 元数据。"
    }

    $businessProject = Get-Content -LiteralPath (
        Join-Path $extractRoot "content/templates/LimeMeta.Service/LimeMetaService/LimeMetaService.csproj") -Raw
    if ($businessProject -match 'PackageReference\s+Include="LimeMeta(?:\.GraphQL)?"' -or
        $businessProject -notmatch 'ProjectReference\s+Include="\.\.\\LimeMeta\\LimeMeta\.csproj"') {
        throw "业务项目没有正确使用框架源码 ProjectReference。"
    }

    $developmentConfig = Get-ChildItem -LiteralPath $extractRoot -Recurse -Filter "appsettings.Development.yml" -Force |
        Select-Object -First 1
    if (-not $developmentConfig -or
        (Get-Content -LiteralPath $developmentConfig.FullName -Raw) -notmatch "DataType:\s*['`"]?MySql") {
        throw "模板默认数据库不是 MySQL。"
    }
}
finally {
    Remove-Item -LiteralPath $extractRoot -Recurse -Force
}

Write-Host "源码内置模板 NuGet 包内容检查通过。"
