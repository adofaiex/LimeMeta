# LimeMeta

LimeMeta 是一个面向 .NET 10 的模型驱动后端框架。它把 FreeSql 数据访问、数据库结构同步、GraphQL 自动查询与修改、FastEndpoints、Logic 生命周期、JWT 认证、文件存储和 WebSocket 组合在一起。

> 本仓库及其 NuGet 包仅供 `adofaiex` 组织授权成员内部使用，禁止向组织外分发。

## 安装模板

首次使用时，先在当前用户的 NuGet 配置中登记组织包源。不要把 PAT 写入仓库或项目配置：

```powershell
$env:GITHUB_USER = "你的 GitHub 用户名"
$env:GITHUB_PACKAGES_TOKEN = "具有 read:packages 权限的 classic PAT"
$env:NuGetPackageSourceCredentials_adofaiex = "Username=$env:GITHUB_USER;Password=$env:GITHUB_PACKAGES_TOKEN"

dotnet nuget add source "https://nuget.pkg.github.com/adofaiex/index.json" --name adofaiex
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
cd MyService
```

PAT 对应的 GitHub 账号还必须拥有 `adofaiex/LimeMeta` 的读取权限。上述凭据环境变量只对当前终端有效；请勿提交包含 `packageSourceCredentials`、PAT 或明文密码的 `NuGet.config`。

模板默认使用 MySQL。先创建数据库并修改开发连接串：

```sql
CREATE DATABASE limemeta_service CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

开发配置位于：

```text
MyService.WebAPI/appsettings.Development.yml
```

修改 `LimeMeta.ConnectionString` 后运行：

```powershell
dotnet run --project MyService.WebAPI
```

默认地址：

- GraphQL：`http://127.0.0.1:6675/api/gql`
- WebSocket：`ws://127.0.0.1:6675/api/ws`

生成项目自带完整中文开发手册，涵盖内置用户/角色/部门/权限、模型与 DTO、自动 GraphQL、Logic、新增 HTTP/GraphQL/WebSocket 接口、种子、文件和部署。也可直接阅读：

- [模板开发指南](templates/LimeMeta.Service/README.md)
- [框架结构与内置能力](templates/LimeMeta.Service/docs/01-overview.md)
- [模型、DTO 与自动 GraphQL](templates/LimeMeta.Service/docs/02-models-and-graphql.md)
- [用户、角色、权限与安全](templates/LimeMeta.Service/docs/03-users-and-authorization.md)
- [Logic、接口与扩展](templates/LimeMeta.Service/docs/04-logic-and-extensions.md)
- [配置、种子、文件与部署](templates/LimeMeta.Service/docs/05-configuration-and-deployment.md)

## 仓库组成

```text
LimeMeta/
├── LimeMeta/                  核心框架包
├── LimeMeta.GraphQL/          GraphQL 扩展包
├── LimeMeta.WebAPI/           框架开发宿主
├── templates/LimeMeta.Service dotnet new 业务模板
└── LimeMeta.Templates.csproj  模板 NuGet 包
```

业务项目只引用 NuGet 包，不复制框架源码：

```xml
<PackageReference Include="LimeMeta" Version="1.0.0" />
<PackageReference Include="LimeMeta.GraphQL" Version="1.0.0" />
```

## 模型与 DTO

模型继承 `BaseObject`、`BaseAudit` 或 `BaseParentChildren`，并带 `[Table]`。每个模型需要同命名空间、同前缀的 DTO。

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

框架会生成：

```text
Article
ArticleAggr
insertArticle
updateArticle
deleteArticle
```

查询支持分页、HotChocolate Filtering、Sorting、导航属性和聚合。

## Logic 生命周期

```csharp
namespace MyService.Logics;

using LimeMeta.Logics;
using MyService.Models;

public sealed class ArticleLogic : BaseLogic<Article>
{
    public ArticleLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeInsert += (_, args) =>
        {
            foreach (var article in args.Objs)
            {
                article.Title = article.Title.Trim();
            }
        };
    }
}
```

支持 Select、Insert、Update、Delete 的 Before/After 事件。业务程序集由模板中的 `AddLimeMetaModule` 自动注册。

## 认证与授权

登录：

```graphql
mutation {
  login(username: "admin", password: "你的密码") {
    token
    name
  }
}
```

后续请求添加：

```text
Authorization: Bearer <token>
```

密码由服务端 BCrypt 处理，每个密码拥有独立随机盐。密码哈希不会出现在 GraphQL Schema、DTO 或响应中。

自动模型操作通过 `ILimeMetaAuthorizationService` 授权：

- 普通业务模型默认允许已认证用户操作。
- LimeMeta 内置系统模型的修改默认仅允许管理员。
- 业务项目可以替换该服务实现更细粒度的权限策略。

专用用户 Mutation：

- `createUser`
- `updateUser`
- `deleteUser`
- `changePassword`
- `resetUserPassword`

内置 `login` 返回 `name` 和 `token`。业务项目可以通过 `BeforeLogin`、`AfterLogin`、`GeneratingJwt` 扩展风控、审计和 JWT Claim；需要返回头像、部门、角色、权限或业务资料时，新增自己的 GraphQL 登录 Mutation 并在内部调用 `UserLogic.Login`。完整示例见[用户权限文档](templates/LimeMeta.Service/docs/03-users-and-authorization.md)。

## 配置

生产环境必须显式提供数据库连接串、管理员初始密码和至少 32 字节的 JWT 密钥。推荐通过环境变量注入：

```bash
export LimeMeta__ConnectionString="Server=127.0.0.1;Port=3306;Database=app;Uid=app;Pwd=strong-password;Charset=utf8mb4;"
export LimeMeta__AdminUserPassword="strong-admin-password"
export LimeMeta__JwtSignKey="replace-with-at-least-32-random-bytes"
```

检测到缺失值、过短 JWT 密钥或生产示例密码时，应用会拒绝启动。

主要配置：

```yaml
LimeMeta:
  ConnectionString: ""
  DataType: "MySql"
  AdminUserName: "admin"
  AdminUserPassword: ""
  JwtSignKey: ""
  JwtExpires: 86400000
  AutoSyncSchema: true
  LoadSeedOnStartup: true
  FileStore:
    Provider: "Local"
    Local:
      Path: "./FileStore"
      Count: 8192
  WebSocket:
    Path: "/api/ws"
    MaxMessageSize: 1048576
```

`access_token` 查询参数只在 WebSocket 路径接受；普通 HTTP JWT 使用 `Authorization`，AppKey 使用 `x-limemeta-app-key` 请求头。

## PostgreSQL

框架仍支持 PostgreSQL。修改配置：

```yaml
LimeMeta:
  DataType: "PostgreSQL"
  ConnectionString: "Host=127.0.0.1;Port=5432;Database=app;Username=app;Password=strong-password"
```

模板同时引用 MySQL 和 PostgreSQL Provider。

## 种子数据与建表

启动顺序：

```text
Seed/BeforeUpdateSchema.sql
→ FreeSql 同步模型表结构
→ Seed/AfterUpdateSchema.sql
→ 初始化管理员/角色/权限
→ 加载模型 YAML
```

模型种子文件必须命名为：

```text
Seed/<ModelType>.yaml
```

例如 `Article` 对应 `Seed/Article.yaml`。

## 文件与 WebSocket

内置文件接口：

- `POST /api/file/upload`
- `GET /api/file/download?id=<guid>`

支持本地存储和 123 云盘 CLI。

WebSocket 控制器：

```csharp
[WsController]
public sealed class NoticeWs
{
    [WsMessage("notice.ping")]
    public object Ping(LimeMetaWebSocketContext context)
        => new { ok = true, context.Connection.Id };
}
```

## 构建与打包

```powershell
dotnet restore
dotnet build LimeMeta.sln -c Release
dotnet test LimeMeta.sln -c Release
.\pack.bat
```

`.nuget/` 会生成三个 `.nupkg`，两个框架包同时生成 `.snupkg`。

正式发布只由 `v*` Git tag 触发 GitHub Actions，并使用工作流自带的短期 `GITHUB_TOKEN` 发布到 `adofaiex` 的私有 GitHub Packages。
维护者的首次发布配置、检查门槛和标签步骤见 [RELEASING.md](RELEASING.md)。

## 版本与兼容性

LimeMeta 使用 Semantic Versioning：

- Patch：兼容修复。
- Minor：向后兼容功能。
- Major：公共 API 破坏性变更。

`1.0.0` 只支持 `net10.0`。模板默认固定引用与模板包相同的框架版本。

## 安全

请勿在普通 Issue 中披露安全漏洞。内部报告方式见 [SECURITY.md](SECURITY.md)。

## 参与贡献

组织内贡献流程见 [CONTRIBUTING.md](CONTRIBUTING.md)，协作行为规范见 [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)。

## 许可证

Copyright 2026 adofaiex. All rights reserved.

这是组织内部专有软件，使用与分发限制见 [LICENSE](LICENSE)。
