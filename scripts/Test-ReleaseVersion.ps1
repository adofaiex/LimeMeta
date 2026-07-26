[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Tag,
    [string]$Commit = "HEAD"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ($Tag -notmatch '^v(?<version>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)$') {
    throw "发布标签必须是 v 开头的 SemVer，例如 v1.0.0。"
}
$version = $Matches.version

[xml]$buildProps = Get-Content -LiteralPath (Join-Path $repositoryRoot "Directory.Build.props") -Raw
$repositoryVersion = [string]$buildProps.Project.PropertyGroup.VersionPrefix
if ($repositoryVersion -ne $version) {
    throw "标签版本 $version 与 Directory.Build.props 中的 $repositoryVersion 不一致。"
}

& git -C $repositoryRoot fetch origin main --no-tags
if ($LASTEXITCODE -ne 0) {
    throw "无法获取 origin/main。"
}
& git -C $repositoryRoot merge-base --is-ancestor $Commit origin/main
if ($LASTEXITCODE -ne 0) {
    throw "标签提交 $Commit 不属于 main 分支。"
}

Write-Host "模板包发布版本 $version 和 main 分支归属检查通过。"
