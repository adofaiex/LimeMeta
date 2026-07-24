# 贡献指南

感谢参与 LimeMeta。

## 开发环境

- .NET SDK 10.0.302 或兼容的 .NET 10 SDK。
- MySQL 8.x。
- PostgreSQL 16 或更高版本。

## 工作流程

1. Fork 仓库并从 `main` 创建功能分支。
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

安全漏洞请按 [SECURITY.md](SECURITY.md) 私密报告，不要公开提交利用代码。

提交贡献即表示你有权提交相关内容，并同意贡献按 Apache-2.0 许可发布。
