# 贡献指南

本文适用于 `adofaiex` 组织内的 LimeMeta 协作。

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

## 公共 API

`LimeMeta` 和 `LimeMeta.GraphQL` 从 1.0 开始遵循 Semantic Versioning。新增公共 API 必须有 XML 文档和测试；删除或改变既有公共 API 只能进入新的主版本。

## 安全问题

安全漏洞请按 [SECURITY.md](SECURITY.md) 在组织内部报告，不要在普通 Issue 中提交利用代码。

提交代码即表示你有权提供相关内容，并同意其作为 `adofaiex` 的内部专有代码管理；不得擅自向组织外分发。
