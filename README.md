# LimeMeta

LimeMeta 是一个面向 .NET 10 的模型驱动后端框架。它把 FreeSql 数据访问、数据库结构同步、GraphQL 自动查询与修改、FastEndpoints、Logic 生命周期、JWT 认证、文件存储和 WebSocket 组合在一起。

LimeMeta 通过项目模板分发。模板生成的解决方案直接包含 `LimeMeta` 与 `LimeMeta.GraphQL` 的完整可编译源码，不再引用这两个框架 NuGet 包。生成后可以在业务仓库中直接阅读、调试和修改框架实现。

## 创建项目

从 NuGet.org 安装唯一保留的模板包：

```powershell
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
cd MyService
```

模板默认使用 MySQL。创建数据库并修改 `MyService.WebAPI/appsettings.Development.yml` 中的连接串：

```sql
CREATE DATABASE my_service CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

```powershell
dotnet run --project MyService.WebAPI
```

默认地址：

- GraphQL：`http://127.0.0.1:6675/api/gql`
- WebSocket：`ws://127.0.0.1:6675/api/ws`

## 生成后的结构

```text
MyService/
├── LimeMeta/                 框架核心源码
├── LimeMeta.GraphQL/         GraphQL 框架源码
├── MyService/                业务模型、DTO、Logic 和服务
├── MyService.WebAPI/         ASP.NET Core 宿主、配置和 Seed
├── docs/                     中文开发手册
├── LICENSE
├── NOTICE
└── MyService.sln
```

业务项目通过源码项目引用使用框架：

```xml
<ProjectReference Include="..\LimeMeta\LimeMeta.csproj" />
<ProjectReference Include="..\LimeMeta.GraphQL\LimeMeta.GraphQL.csproj" />
```

这两个框架目录是创建项目时的源码快照，之后归生成项目所有。更新模板不会自动覆盖已有项目，也不提供自动上游同步；业务团队可以保留本地改动，或在需要时人工比较新模板源码。

## 模型与自动 GraphQL

模型继承 `BaseObject`、`BaseAudit` 或 `BaseParentChildren<T>`，添加 FreeSql `[Table]`，并在相同命名空间定义 `<ModelName>Dto`：

```csharp
namespace MyService.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Models;

[Table(Name = "article")]
public sealed class Article : BaseAudit
{
    [Column(Name = "title", StringLength = 200)]
    public string Title { get; set; } = string.Empty;
}

public sealed class ArticleDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
}
```

框架自动生成：

```text
Article
ArticleAggr
insertArticle
updateArticle
deleteArticle
```

查询支持分页、HotChocolate Filtering、Sorting、导航属性和聚合。业务程序集通过模板中的 `AddLimeMetaModule` 注册。

如果模型仍需参与数据库结构同步、Seed、Logic 和 `ILimeMeta` 数据操作，但不应自动生成 GraphQL 根字段，可以添加：

```csharp
using LimeMeta.Attributes;

[Table(Name = "internal_job")]
[DisableGraphQL]
public sealed class InternalJob : BaseAudit
{
}
```

这会关闭该模型的自动查询、聚合及增删改 Mutation。模型仍需按约定定义 `<ModelName>Dto`。若其他已公开模型通过导航属性引用它，还应在对应导航属性上使用 HotChocolate 的 `[GraphQLIgnore]`。

## 认证、配置与存储

登录：

```graphql
mutation {
  login(username: "admin", password: "你的密码") {
    token
    name
  }
}
```

后续请求使用 `Authorization: Bearer <token>`。密码由 BCrypt 处理；自动模型操作通过 `ILimeMetaAuthorizationService` 授权，生产项目应按业务要求替换默认授权策略。

生产环境必须显式提供数据库连接串、管理员初始密码和至少 32 字节的 JWT 密钥：

```bash
export LimeMeta__ConnectionString="Server=127.0.0.1;Port=3306;Database=app;Uid=app;Pwd=strong-password;Charset=utf8mb4;"
export LimeMeta__AdminUserPassword="strong-admin-password"
export LimeMeta__JwtSignKey="replace-with-at-least-32-random-bytes"
```

框架支持 MySQL、PostgreSQL、本地文件存储和 123 云盘 CLI。内置 HTTP 文件接口为：

- `POST /api/file/upload`
- `GET /api/file/download?id=<guid>`

## 仓库组成

```text
LimeMeta/                       核心框架源码项目
LimeMeta.GraphQL/               GraphQL 源码项目
LimeMeta.WebAPI/                框架开发与数据库冒烟宿主
LimeMeta.Tests/                 自动化测试
templates/LimeMeta.Service/     业务模板骨架和中文手册
LimeMeta.Templates.csproj       唯一发布的 NuGet 模板包
```

框架项目本身不可打包。模板构建时直接把仓库中的两个框架源码项目映射到模板内容，避免在仓库内维护重复副本。

## 构建与验证

```powershell
dotnet restore LimeMeta.sln
dotnet format LimeMeta.sln --verify-no-changes --no-restore
dotnet build LimeMeta.sln -c Release --no-restore -warnaserror
dotnet test LimeMeta.sln -c Release --no-build --no-restore
dotnet pack LimeMeta.Templates.csproj -c Release -o .artifacts/packages
.\scripts\Test-PackageContents.ps1 -PackageDirectory .artifacts/packages -Version 1.0.3
.\scripts\Test-TemplatePackage.ps1 -PackageDirectory .artifacts/packages -Version 1.0.3
```

CI 还会在 MySQL 与 PostgreSQL 上执行真实登录、CRUD、聚合、Logic 和授权冒烟测试。

正式发布由 `v*` Git tag 触发 GitHub Actions，通过 NuGet OIDC Trusted Publishing 只发布 `LimeMeta.Templates`，并创建带模板包和 SHA-256 校验文件的 GitHub Release。维护说明见 [RELEASING.md](RELEASING.md)。

## 文档与许可证

生成项目自带完整中文手册：

- [模板开发指南](templates/LimeMeta.Service/README.md)
- [框架结构与内置能力](templates/LimeMeta.Service/docs/01-overview.md)
- [模型、DTO 与自动 GraphQL](templates/LimeMeta.Service/docs/02-models-and-graphql.md)
- [用户、角色、权限与安全](templates/LimeMeta.Service/docs/03-users-and-authorization.md)
- [Logic、接口与扩展](templates/LimeMeta.Service/docs/04-logic-and-extensions.md)
- [配置、种子、文件与部署](templates/LimeMeta.Service/docs/05-configuration-and-deployment.md)

项目使用 Apache-2.0 许可证。安全问题请按 [SECURITY.md](SECURITY.md) 私密报告。
