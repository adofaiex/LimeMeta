# 贡献指南

本文适用于 LimeMeta 的公开协作。

## 开发环境

- .NET SDK 10.0.302 或兼容的 .NET 10 SDK。
- MySQL 8.x。
- PostgreSQL 16 或更高版本。

## 工作流程

1. 从 `main` 创建功能分支；不要把代码复制到组织外的 Fork。
2. 保持改动聚焦，并为行为变化补充测试和中文文档。
3. 执行：

   ```powershell
   dotnet restore
   dotnet format --verify-no-changes
   dotnet build LimeMeta.sln -c Release
   dotnet test LimeMeta.sln -c Release
   ```

4. 提交 Pull Request，说明动机、兼容性、安全影响和验证方式。

## 框架源码兼容性

`LimeMeta` 和 `LimeMeta.GraphQL` 不再作为独立 NuGet 包发布，但模板使用者会在业务仓库中持有它们的源码快照。改变公共类型或运行行为时仍必须补充 XML 文档、测试和迁移说明，避免新模板生成的项目出现无说明的破坏性变化。

## 安全问题

安全漏洞请按 [SECURITY.md](SECURITY.md) 报告，不要在普通 Issue 中提交可复现利用代码。

提交代码即表示你同意本仓库采用 Apache-2.0 许可发布，并遵守社区协作规范。
