# 发布指南

LimeMeta 的正式包只通过 `.github/workflows/release.yml` 发布到 NuGet.org。不要在本机保存或使用长期 NuGet API Key。

## 一次性平台配置

1. 在 GitHub 撤销历史中曾出现的 PAT，并确认所有旧克隆都已删除或重新克隆。
2. 仓库公开前开启：
   - `main` 分支保护与 CI 必需检查；
   - 禁止强推和直接提交；
   - Dependabot、CodeQL、秘密扫描、Push protection；
   - 私密漏洞报告。
3. 在 GitHub 创建名为 `nuget.org` 的 Environment，限制为 `v*` 标签，必要时加入维护者审批。
4. 在 NuGet.org 的 `memsys-lizi` 账号下为以下三个 Package ID 建立 Trusted Publishing policy：
   - `LimeMeta`
   - `LimeMeta.GraphQL`
   - `LimeMeta.Templates`
5. Policy 必须绑定：
   - Owner：`memsys-lizi`
   - Repository：`LimeMeta`
   - Workflow：`release.yml`
   - Environment：`nuget.org`

如果 NuGet.org 支持“待发布 Policy”，应在首次推送前创建；否则按 NuGet.org 首次包所有权流程操作，并立即切换到 Trusted Publishing。

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

MySQL 与 PostgreSQL 的数据库冒烟作业也必须通过。流水线会验证建表、管理员初始化、健康检查、登录、CRUD、聚合、Logic、系统模型授权和密码修改。

## 创建发布

1. 更新 `CHANGELOG.md` 的发布日期。
2. 确认 `Directory.Build.props`、模板 `limeMetaVersion` 和计划标签完全一致。
3. 确认三个 Package ID 在 NuGet.org 的所有权正确。
4. 在 `main` 最新提交上创建并推送带签名标签：

   ```powershell
   git tag -s v1.0.0 -m "LimeMeta 1.0.0"
   git push origin v1.0.0
   ```

发布工作流会再次运行完整检查，只构建一次发布二进制，并依次发布 `LimeMeta`、`LimeMeta.GraphQL`、`LimeMeta.Templates`。GitHub Release 会保存 `.nupkg`、`.snupkg`、SHA-256 和 SPDX SBOM；GitHub Artifact Attestations 保存构建来源证明。

发布失败时不要重复使用相同版本上传不同内容。修复后增加版本号并重新走完整流程。
