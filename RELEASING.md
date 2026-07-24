# 组织内部发布指南

LimeMeta 的正式包只通过 `.github/workflows/release.yml` 发布到
`adofaiex` 的私有 GitHub Packages。禁止发布到 NuGet.org、个人包源或
其他公共 Registry，也不要在仓库中保存 PAT。

## 发布目标

- 源码仓库：`https://github.com/adofaiex/LimeMeta`
- NuGet 源：`https://nuget.pkg.github.com/adofaiex/index.json`
- 包：`LimeMeta`、`LimeMeta.GraphQL`、`LimeMeta.Templates`
- 可见性：Private，并继承 `adofaiex/LimeMeta` 的访问权限
- 发布凭据：GitHub Actions 为当前仓库签发的短期 `GITHUB_TOKEN`

## 一次性组织配置

1. 保持仓库为 Private，并用组织 Team 管理 Read、Write、Maintain 和
   Admin 权限。
2. 在仓库 **Settings → Actions → General** 中允许工作流获得所声明的
   `packages: write` 和 `contents: write` 权限。
3. 第一次发布后，在三个 Package 的设置中确认：
   - 包关联到 `adofaiex/LimeMeta`；
   - 包继承仓库访问权限；
   - 未授予组织外部账号或仓库访问权限。
4. 当前组织为 GitHub Free，私有仓库不能上传 CodeQL 结果，因此不要设置
   `ENABLE_CODEQL`。未来升级并启用 GitHub Code Security 后，再设置
   `ENABLE_CODEQL=true`。
5. 为 `main` 配置适合当前组织套餐的分支规则，至少要求 CI 通过并禁止强推。

GitHub Free for organizations 的私有 Packages 与 Actions Artifacts 共享 500 MB
存储额度，每月包含 1 GB 包下载流量。发布前应在组织 Billing 中设置预算提醒并
定期清理不再使用的预发布版本。

## 开发者安装凭据

GitHub 的 NuGet Registry 要求客户端认证。每位使用者必须：

1. 拥有 `adofaiex/LimeMeta` 的读取权限。
2. 创建具有 `read:packages` 权限的 classic PAT；如果组织启用了 SAML SSO，
   还要为该 Token 授权组织访问。
3. 在用户级 NuGet 配置中登记源，并通过当前终端环境变量提供凭据：

   ```powershell
   $env:GITHUB_USER = "你的 GitHub 用户名"
   $env:GITHUB_PACKAGES_TOKEN = "具有 read:packages 权限的 classic PAT"
   $env:NuGetPackageSourceCredentials_adofaiex = "Username=$env:GITHUB_USER;Password=$env:GITHUB_PACKAGES_TOKEN"

   dotnet nuget add source "https://nuget.pkg.github.com/adofaiex/index.json" --name adofaiex
   dotnet new install LimeMeta.Templates
   ```

不得把 Token、`packageSourceCredentials` 或明文密码提交到任何仓库。

## 发布门槛

只有以下检查全部通过，才可创建标签：

```powershell
dotnet restore LimeMeta.sln
dotnet format LimeMeta.sln --verify-no-changes --no-restore
dotnet build LimeMeta.sln -c Release --no-restore -warnaserror
dotnet test LimeMeta.sln -c Release --no-build --no-restore
pwsh ./scripts/Test-Secrets.ps1

dotnet pack LimeMeta/LimeMeta.csproj -c Release --no-build -o .artifacts/packages
dotnet pack LimeMeta.GraphQL/LimeMeta.GraphQL.csproj -c Release --no-build -o .artifacts/packages
dotnet pack LimeMeta.Templates.csproj -c Release --no-build -o .artifacts/packages
pwsh ./scripts/Test-PackageContents.ps1 -PackageDirectory .artifacts/packages
pwsh ./scripts/Test-TemplatePackage.ps1 -PackageDirectory .artifacts/packages
```

MySQL 与 PostgreSQL 的数据库冒烟作业也必须通过。流水线会验证建表、管理员
初始化、健康检查、登录、CRUD、聚合、Logic、系统模型授权和密码修改。

## 创建内部发布

1. 更新 `CHANGELOG.md` 的发布日期。
2. 确认 `Directory.Build.props`、模板 `limeMetaVersion` 和计划标签完全一致。
3. 确认 CI 已通过，并确认本次版本尚未发布到组织包源。
4. 在 `main` 最新提交上创建并推送带签名标签：

   ```powershell
   git tag -s v1.0.0 -m "LimeMeta 1.0.0"
   git push origin v1.0.0
   ```

发布工作流会再次运行完整检查，只构建一次发布二进制，并依次发布
`LimeMeta`、`LimeMeta.GraphQL`、`LimeMeta.Templates`。私有 GitHub Release
会保存 `.nupkg`、`.snupkg`、SHA-256 和 SPDX SBOM。当前组织套餐不支持私有
仓库 Artifact Attestations，因此发布流程不生成证明；升级到 GitHub Enterprise
Cloud 后再启用。

发布失败时，不得用相同版本上传不同内容。确认已经成功上传的包后，修复问题、
增加版本号并重新运行完整发布流程。
