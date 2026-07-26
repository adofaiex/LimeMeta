# 发布 LimeMeta 模板

LimeMeta 只发布 `LimeMeta.Templates`。`LimeMeta` 与 `LimeMeta.GraphQL` 是不可打包的源码项目，由模板包直接嵌入生成结果。

## 发布前检查

- 本地分支与 `origin/main` 同步，工作区干净。
- `Directory.Build.props` 的 `VersionPrefix` 与准备创建的 `v*` 标签一致。
- NuGet.org 已为 `LimeMeta.Templates` 配置 GitHub OIDC Trusted Publishing。

执行：

```powershell
dotnet restore LimeMeta.sln
dotnet format LimeMeta.sln --verify-no-changes --no-restore
dotnet build LimeMeta.sln -c Release --no-restore -warnaserror
dotnet test LimeMeta.sln -c Release --no-build --no-restore

dotnet pack LimeMeta.Templates.csproj -c Release --no-build -o .artifacts/packages
.\scripts\Test-PackageContents.ps1 -PackageDirectory .artifacts/packages -Version 1.0.3
.\scripts\Test-TemplatePackage.ps1 -PackageDirectory .artifacts/packages -Version 1.0.3
```

验收必须确认模板只生成四个源码项目，不存在 LimeMeta 框架 `PackageReference`，并能在修改内置框架源码后重新构建。

## 正式发布

```powershell
git tag -s v1.0.3 -m "LimeMeta Templates 1.0.3"
git push origin v1.0.3
```

`.github/workflows/release.yml` 会：

1. 校验标签版本、提交属于 `main`。
2. 执行格式、构建、测试、漏洞、许可证、秘密和双数据库检查。
3. 只构建并审计 `LimeMeta.Templates.<version>.nupkg`。
4. 安装模板、生成四项目源码解决方案并完成两次构建。
5. 生成 SHA-256 校验文件。
6. 使用 NuGet OIDC 临时密钥发布模板包。
7. 创建包含模板包和校验文件的 GitHub Release。

禁止恢复长期 NuGet API Key，也不要重新发布两个框架包。
