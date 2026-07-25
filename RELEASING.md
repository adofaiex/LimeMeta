# 公开发布指南

LimeMeta 的正式包通过 `.github/workflows/release.yml` 发布到 NuGet.org。

## 发布目标

- 源码仓库：`https://github.com/adofaiex/LimeMeta`
- NuGet 源：`https://api.nuget.org/v3/index.json`
- 包：`LimeMeta`、`LimeMeta.GraphQL`、`LimeMeta.Templates`
- 可见性：Public（公开读取）
- 版本要求：`Directory.Build.props` 的 `VersionPrefix`、模板 `limeMetaVersion`
  与 Tag 必须完全一致

## 必要配置

1. 在仓库 **Settings → Secrets and variables → Actions → Repository secrets** 中配置
   Trusted Publishing 对应的 NuGet 发布身份（OIDC 发布不需要长期 API Key）。
2. 在 `main` 分支配置至少：
   - CI 必须通过；
   - 不允许直接推送（禁止强制推送）；
   - 发布分支要求 `main` 最近成功构建并可回滚。

## 发布门槛（本地或 CI）

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

MySQL 与 PostgreSQL 数据库冒烟测试也必须通过，确保建表、登录、CRUD、聚合、
Logic、系统模型授权和密码重置链路可复现。

## 创建发布

1. 更新 `CHANGELOG.md`。
2. 更新 `Directory.Build.props`、`templates/LimeMeta.Service/.template.config/template.json`
   与发布 Tag（如 `v1.0.0`）保持一致。
3. 在 `main` 最新提交上创建并推送 Tag：

   ```powershell
   git tag v1.0.0 -m "LimeMeta 1.0.0"
   git push origin v1.0.0
   ```

4. 推送 Tag 后 GitHub Actions 的 `release` 工作流自动触发并完成：
   - 全历史扫描与依赖漏洞扫描
   - 全量构建/测试
   - MySQL 与 PostgreSQL 冒烟测试
   - 包审计、模板烟雾测试
   - `.nupkg`、`.snupkg`、SHA-256 和 SPDX SBOM 生成
   - 推送到 NuGet.org 三个包
   - 创建 GitHub Release 并附带校验文件

## 异常处理

发布失败后，不得用同版本重复发布不同内容。确认问题修复后请先提
升版本并重新打新 Tag。

