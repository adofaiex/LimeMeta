[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("MySql", "PostgreSQL")]
    [string]$DataType,
    [Parameter(Mandatory)]
    [string]$ConnectionString,
    [int]$Port = 5180
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "LimeMeta.WebAPI/LimeMeta.WebAPI.csproj"
$baseUrl = "http://127.0.0.1:$Port"
$logPath = Join-Path ([System.IO.Path]::GetTempPath()) ("limemeta-db-" + [guid]::NewGuid().ToString("N") + ".log")

$environment = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    ASPNETCORE_URLS = $baseUrl
    Urls = $baseUrl
    LimeMeta__DataType = $DataType
    LimeMeta__ConnectionString = $ConnectionString
    LimeMeta__AdminUserName = "admin"
    LimeMeta__AdminUserPassword = "Local-Smoke-Password-2026!"
    LimeMeta__JwtSignKey = "local-smoke-jwt-key-that-is-longer-than-32-bytes"
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = "dotnet"
$startInfo.ArgumentList.Add("run")
$startInfo.ArgumentList.Add("--project")
$startInfo.ArgumentList.Add($projectPath)
$startInfo.ArgumentList.Add("-c")
$startInfo.ArgumentList.Add("Release")
$startInfo.ArgumentList.Add("--no-build")
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
foreach ($entry in $environment.GetEnumerator()) {
    $startInfo.Environment[$entry.Key] = $entry.Value
}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$null = $process.Start()
$stdoutTask = $process.StandardOutput.ReadToEndAsync()
$stderrTask = $process.StandardError.ReadToEndAsync()
$succeeded = $false

function Invoke-GraphQL {
    param(
        [Parameter(Mandatory)]
        [string]$Query,
        [string]$Token
    )

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers.Authorization = "Bearer $Token"
    }

    $requestBody = @{ query = $Query } | ConvertTo-Json -Depth 20
    return Invoke-RestMethod `
        -Uri "$baseUrl/api/gql" `
        -Method Post `
        -ContentType "application/json" `
        -Headers $headers `
        -Body $requestBody
}

try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($process.HasExited) {
            break
        }
        try {
            $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -TimeoutSec 2
            $ready = $true
            break
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $ready) {
        throw "$DataType 服务未能通过健康检查。"
    }

    $response = Invoke-GraphQL -Query 'mutation { login(username: "admin", password: "Local-Smoke-Password-2026!") { token } }'
    if ($response.errors -or [string]::IsNullOrWhiteSpace($response.data.login.token)) {
        throw "$DataType 管理员登录失败：$($response | ConvertTo-Json -Depth 8 -Compress)"
    }
    $adminToken = $response.data.login.token

    $suffix = [guid]::NewGuid().ToString("N")
    $roleId = [guid]::NewGuid()
    $roleName = "smoke-role-$suffix"
    $insertRole = Invoke-GraphQL -Token $adminToken -Query @"
mutation {
  insertRole(objs: [{
    id: "$roleId"
    name: "$roleName"
    sn: 999
  }])
}
"@
    if ($insertRole.errors -or
        $insertRole.data.insertRole.Count -ne 1 -or
        $insertRole.data.insertRole[0] -ne $roleId.ToString()) {
        throw "$DataType Role 新增失败：$($insertRole | ConvertTo-Json -Depth 8 -Compress)"
    }

    $roleQuery = Invoke-GraphQL -Token $adminToken -Query @"
query {
  Role(where: { id: { eq: "$roleId" } }) {
    total
    items {
      id
      name
      path
      created
      creatorId
    }
  }
}
"@
    $role = $roleQuery.data.Role.items | Select-Object -First 1
    if ($roleQuery.errors -or
        $roleQuery.data.Role.total -ne 1 -or
        $role.name -ne $roleName -or
        $role.path -notlike "*$roleId.*" -or
        $role.created -le 0 -or
        [string]::IsNullOrWhiteSpace($role.creatorId)) {
        throw "$DataType Role 查询或 Logic 验证失败：$($roleQuery | ConvertTo-Json -Depth 8 -Compress)"
    }

    $aggregation = Invoke-GraphQL -Token $adminToken -Query @"
query {
  RoleAggr(
    where: { id: { eq: "$roleId" } }
    fields: [{ type: COUNT, name: "Id" }]
  )
}
"@
    $aggregateRows = @($aggregation.data.RoleAggr)
    if ($aggregation.errors -or
        $aggregateRows.Count -ne 1 -or
        [int]$aggregateRows[0].IdCount -ne 1) {
        throw "$DataType Role 聚合失败：$($aggregation | ConvertTo-Json -Depth 8 -Compress)"
    }

    $updatedRoleName = "$roleName-updated"
    $updateRole = Invoke-GraphQL -Token $adminToken -Query @"
mutation {
  updateRole(objs: [{
    id: "$roleId"
    name: "$updatedRoleName"
  }])
}
"@
    if ($updateRole.errors -or $updateRole.data.updateRole -ne 1) {
        throw "$DataType Role 更新失败：$($updateRole | ConvertTo-Json -Depth 8 -Compress)"
    }

    $username = "smoke-user-$suffix"
    $initialPassword = "Smoke-Initial-Password-2026!"
    $newPassword = "Smoke-Changed-Password-2026!"
    $createUser = Invoke-GraphQL -Token $adminToken -Query @"
mutation {
  createUser(
    name: "Smoke User"
    username: "$username"
    password: "$initialPassword"
    roleIds: []
  )
}
"@
    if ($createUser.errors -or
        [string]::IsNullOrWhiteSpace($createUser.data.createUser)) {
        throw "$DataType 用户创建失败：$($createUser | ConvertTo-Json -Depth 8 -Compress)"
    }
    $userId = $createUser.data.createUser

    $userLogin = Invoke-GraphQL -Query @"
mutation {
  login(username: "$username", password: "$initialPassword") {
    token
  }
}
"@
    if ($userLogin.errors -or
        [string]::IsNullOrWhiteSpace($userLogin.data.login.token)) {
        throw "$DataType 普通用户登录失败：$($userLogin | ConvertTo-Json -Depth 8 -Compress)"
    }
    $userToken = $userLogin.data.login.token

    $forbiddenRoleId = [guid]::NewGuid()
    $forbiddenMutation = Invoke-GraphQL -Token $userToken -Query @"
mutation {
  insertRole(objs: [{
    id: "$forbiddenRoleId"
    name: "forbidden-$suffix"
    sn: 1000
  }])
}
"@
    if (-not $forbiddenMutation.errors) {
        throw "$DataType 非管理员修改系统模型未被拒绝。"
    }

    $changePassword = Invoke-GraphQL -Token $userToken -Query @"
mutation {
  changePassword(
    currentPassword: "$initialPassword"
    newPassword: "$newPassword"
  )
}
"@
    if ($changePassword.errors -or -not $changePassword.data.changePassword) {
        throw "$DataType 修改密码失败：$($changePassword | ConvertTo-Json -Depth 8 -Compress)"
    }

    $changedLogin = Invoke-GraphQL -Query @"
mutation {
  login(username: "$username", password: "$newPassword") {
    token
  }
}
"@
    if ($changedLogin.errors -or
        [string]::IsNullOrWhiteSpace($changedLogin.data.login.token)) {
        throw "$DataType 新密码登录失败：$($changedLogin | ConvertTo-Json -Depth 8 -Compress)"
    }

    $deleteUser = Invoke-GraphQL -Token $adminToken -Query @"
mutation {
  deleteUser(userId: "$userId")
}
"@
    if ($deleteUser.errors -or -not $deleteUser.data.deleteUser) {
        throw "$DataType 用户清理失败：$($deleteUser | ConvertTo-Json -Depth 8 -Compress)"
    }

    $deleteRole = Invoke-GraphQL -Token $adminToken -Query @"
mutation {
  deleteRole(ids: ["$roleId"])
}
"@
    if ($deleteRole.errors -or $deleteRole.data.deleteRole -ne 1) {
        throw "$DataType Role 清理失败：$($deleteRole | ConvertTo-Json -Depth 8 -Compress)"
    }

    $succeeded = $true
}
finally {
    if (-not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }
    $output = $stdoutTask.GetAwaiter().GetResult() +
        [Environment]::NewLine +
        $stderrTask.GetAwaiter().GetResult()
    [System.IO.File]::WriteAllText($logPath, $output)
    if ($succeeded) {
        Remove-Item -LiteralPath $logPath -Force
    }
    else {
        Write-Warning "服务日志保存在 $logPath"
    }
}

Write-Host "$DataType 建表、管理员初始化、登录、CRUD、聚合、Logic、授权和密码验证通过。"
