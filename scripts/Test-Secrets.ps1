[CmdletBinding()]
param(
    [switch]$CurrentTreeOnly
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pattern = 'gh[pousr]_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----'
$excludedPaths = @(
    ".git/",
    ".artifacts/",
    ".tools/",
    "bin/",
    "obj/"
)

function Test-ScanLine([string]$line) {
    foreach ($excludedPath in $excludedPaths) {
        if ($line.Replace("\", "/").Contains($excludedPath, [StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }
    if ($line -match $pattern) {
        throw "检测到疑似秘密：$line"
    }
}

Push-Location $repositoryRoot
try {
    $trackedFiles = & git ls-files --cached --others --exclude-standard
    foreach ($file in $trackedFiles) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            continue
        }
        $text = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $file))
        if ($text -match $pattern) {
            throw "当前文件 $file 中检测到疑似秘密。"
        }
    }

    if (-not $CurrentTreeOnly) {
        $commits = & git rev-list --all
        foreach ($commit in $commits) {
            $matches = & git grep -I -n -E $pattern $commit 2>$null
            foreach ($match in $matches) {
                Test-ScanLine $match
            }
        }
    }
}
finally {
    Pop-Location
}

Write-Host "当前文件与 Git 历史秘密模式检查通过。"
