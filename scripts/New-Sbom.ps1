[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputPath,
    [string]$Version = "1.0.0",
    [string]$Commit = "unknown"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$packageJson = & dotnet list (Join-Path $repositoryRoot "LimeMeta.sln") package --include-transitive --format json
if ($LASTEXITCODE -ne 0) {
    throw "无法读取已解析的 NuGet 依赖。"
}
$packageReport = ($packageJson -join [Environment]::NewLine) | ConvertFrom-Json

$dependencies = foreach ($project in $packageReport.projects) {
    foreach ($framework in $project.frameworks) {
        @($framework.topLevelPackages) + @($framework.transitivePackages) | ForEach-Object {
            [pscustomobject]@{
                id = [string]$_.id
                version = [string]$_.resolvedVersion
            }
        }
    }
}
$dependencies = $dependencies |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.id) } |
    Sort-Object id, version -Unique

$rootSpdxId = "SPDXRef-Package-LimeMeta"
$packages = @(
    [ordered]@{
        SPDXID = $rootSpdxId
        name = "LimeMeta"
        versionInfo = $Version
        downloadLocation = "https://www.nuget.org/packages/LimeMeta/$Version"
        filesAnalyzed = $false
        licenseConcluded = "Apache-2.0"
        licenseDeclared = "Apache-2.0"
        copyrightText = "Copyright 2026 memsys-lizi"
    }
)
$relationships = @()
foreach ($dependency in $dependencies) {
    $safeId = ($dependency.id + "-" + $dependency.version) -replace '[^A-Za-z0-9.-]', '-'
    $spdxId = "SPDXRef-Package-$safeId"
    $packages += [ordered]@{
        SPDXID = $spdxId
        name = $dependency.id
        versionInfo = $dependency.version
        downloadLocation = "https://www.nuget.org/packages/$($dependency.id)/$($dependency.version)"
        filesAnalyzed = $false
        licenseConcluded = "NOASSERTION"
        licenseDeclared = "NOASSERTION"
        copyrightText = "NOASSERTION"
    }
    $relationships += [ordered]@{
        spdxElementId = $rootSpdxId
        relationshipType = "DEPENDS_ON"
        relatedSpdxElement = $spdxId
    }
}

$document = [ordered]@{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "LimeMeta-$Version"
    documentNamespace = "https://github.com/memsys-lizi/LimeMeta/sbom/$Commit"
    creationInfo = [ordered]@{
        created = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        creators = @("Organization: memsys-lizi", "Tool: scripts/New-Sbom.ps1")
    }
    packages = $packages
    relationships = $relationships
}

$absoluteOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($absoluteOutputPath)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}
[System.IO.File]::WriteAllText(
    $absoluteOutputPath,
    ($document | ConvertTo-Json -Depth 8),
    [System.Text.UTF8Encoding]::new($false))

Write-Host "SPDX 2.3 SBOM 已生成：$absoluteOutputPath"
