# LimeMeta.Templates

LimeMeta.Templates 用于创建 .NET 10 模型驱动后端项目。生成的项目默认使用 MySQL，并包含 GraphQL 自动 CRUD、用户/角色/权限、Logic 生命周期、FastEndpoints、文件存储、WebSocket、种子数据和生产配置入口。

## 安装与创建

此包发布在 NuGet.org，安装时无需任何仓库授权。直接执行：

```powershell
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
cd MyService
```

无需把 PAT 或 `packageSourceCredentials` 写入项目仓库。

创建指定框架版本的项目：

```powershell
dotnet new limemeta -n MyService --limeMetaVersion 1.0.0
```

## 生成后从哪里开始

生成目录中的 `README.md` 是完整中文入口手册，内容包括：

- MySQL 首次启动、登录和第一个模型。
- 项目结构、启动顺序和框架内置能力。
- 模型、DTO、关联、树结构和自动 GraphQL。
- 用户、角色、部门、权限、JWT、AppKey 和自定义授权。
- Logic、领域服务、GraphQL 扩展、FastEndpoints 和 WebSocket。
- YAML Seed、文件存储、PostgreSQL 切换、生产配置和部署检查表。

在线查看模板文档：[LimeMeta Service 开发指南](https://github.com/adofaiex/LimeMeta/blob/main/templates/LimeMeta.Service/README.md)。

## 更新或卸载模板

```powershell
dotnet new update
dotnet new uninstall LimeMeta.Templates
```

项目生成后，框架包版本固定写入业务项目的 `.csproj`，不会因为模板更新而自动改变。升级项目前请同时更新 `LimeMeta` 和 `LimeMeta.GraphQL`，并保持版本一致。

