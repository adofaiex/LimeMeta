[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory,
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$expectedPackages = @(
    "LimeMeta.$Version.nupkg",
    "LimeMeta.GraphQL.$Version.nupkg",
    "LimeMeta.Templates.$Version.nupkg"
)
$forbiddenPatterns = @(
    "ghp_[A-Za-z0-9]+",
    "github_pat_[A-Za-z0-9_]+",
    "nuget\.pkg\.github\.com/memsys-lizi",
    "<packageSourceCredentials",
    "ClearTextPassword",
    "Main\.txt",
    "[A-Z]:\\Users\\",
    "/home/[^/]+/"
)

foreach ($fileName in $expectedPackages) {
    $packagePath = Join-Path $packageRoot $fileName
    if (-not (Test-Path -LiteralPath $packagePath)) {
        throw "缺少包：$fileName"
    }

    $extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("limemeta-audit-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $extractRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $packagePath -DestinationPath $extractRoot
        $entries = Get-ChildItem -LiteralPath $extractRoot -Recurse -File -Force
        $relativeEntries = $entries | ForEach-Object {
            [System.IO.Path]::GetRelativePath($extractRoot, $_.FullName).Replace("\", "/")
        }

        foreach ($required in @("LICENSE", "NOTICE", "limemeta-icon.png")) {
            if ($relativeEntries -notcontains $required) {
                throw "$fileName 缺少 $required"
            }
        }

        if ($relativeEntries | Where-Object { $_ -match "(^|/)(bin|obj)/" }) {
            throw "$fileName 含有 bin/obj 生成物。"
        }

        foreach ($entry in $entries) {
            if ($entry.Length -gt 10MB) {
                continue
            }

            $bytes = [System.IO.File]::ReadAllBytes($entry.FullName)
            $text = [System.Text.Encoding]::UTF8.GetString($bytes)
            foreach ($pattern in $forbiddenPatterns) {
                if ($text -match $pattern) {
                    throw "$fileName 的 $($entry.Name) 命中禁止内容：$pattern"
                }
            }
        }

        $nuspec = Get-ChildItem -LiteralPath $extractRoot -Filter "*.nuspec" | Select-Object -First 1
        if (-not $nuspec) {
            throw "$fileName 缺少 nuspec。"
        }
        [xml]$metadataDocument = Get-Content -LiteralPath $nuspec.FullName -Raw
        $metadata = $metadataDocument.package.metadata
        if ($metadata.license.type -ne "file" -or $metadata.license.InnerText -ne "LICENSE") {
            throw "$fileName 的许可证元数据不正确。"
        }
        if ($metadata.icon -ne "limemeta-icon.png" -or [string]::IsNullOrWhiteSpace($metadata.readme)) {
            throw "$fileName 缺少图标或 README 元数据。"
        }
        if ($metadata.repository.type -ne "git" -or
            $metadata.repository.url -ne "https://github.com/adofaiex/LimeMeta" -or
            [string]::IsNullOrWhiteSpace($metadata.repository.commit)) {
            throw "$fileName 缺少正确的 Git 仓库与提交元数据。"
        }

        if ($fileName -eq "LimeMeta.GraphQL.$Version.nupkg") {
            $dependency = $metadata.dependencies.group.dependency |
                Where-Object { $_.id -eq "LimeMeta" } |
                Select-Object -First 1
            if (-not $dependency -or $dependency.version -ne "[$Version]") {
                throw "LimeMeta.GraphQL 必须精确依赖 LimeMeta [$Version]。"
            }
        }

        if ($fileName -eq "LimeMeta.Templates.$Version.nupkg") {
            if ($metadata.packageTypes.packageType.name -ne "Template") {
                throw "模板包缺少 Template 包类型。"
            }
            $templateJson = Get-ChildItem -LiteralPath $extractRoot -Recurse -Filter "template.json" -Force |
                Select-Object -First 1
            if (-not $templateJson) {
                throw "模板包缺少 template.json。"
            }
            $templateText = Get-Content -LiteralPath $templateJson.FullName -Raw
            if ($templateText -notmatch '"defaultValue"\s*:\s*"' + [regex]::Escape($Version) + '"') {
                throw "模板默认 LimeMeta 版本不是 $Version。"
            }
            $developmentConfig = Get-ChildItem -LiteralPath $extractRoot -Recurse -Filter "appsettings.Development.yml" -Force |
                Select-Object -First 1
            if (-not $developmentConfig -or
                (Get-Content -LiteralPath $developmentConfig.FullName -Raw) -notmatch "DataType:\s*['`"]?MySql") {
                throw "模板默认数据库不是 MySQL。"
            }
            if ($relativeEntries | Where-Object { $_ -match "(^|/)NuGet\.config$|Seed/system\.yml$" }) {
                throw "模板包中不应包含私有源配置或无效 system.yml。"
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $extractRoot -Recurse -Force
    }
}

foreach ($frameworkPackage in @("LimeMeta", "LimeMeta.GraphQL")) {
    $symbolsPath = Join-Path $packageRoot "$frameworkPackage.$Version.snupkg"
    if (-not (Test-Path -LiteralPath $symbolsPath)) {
        throw "缺少符号包：$frameworkPackage.$Version.snupkg"
    }
    $symbolsRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("limemeta-symbols-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $symbolsRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $symbolsPath -DestinationPath $symbolsRoot
        if (-not (Get-ChildItem -LiteralPath $symbolsRoot -Recurse -Filter "*.pdb" | Select-Object -First 1)) {
            throw "$frameworkPackage 符号包中缺少 Portable PDB。"
        }
    }
    finally {
        Remove-Item -LiteralPath $symbolsRoot -Recurse -Force
    }
}

Write-Host "三个 NuGet 包的内容检查通过。"
