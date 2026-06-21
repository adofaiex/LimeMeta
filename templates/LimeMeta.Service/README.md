# LimeMetaService 后端开发说明

这是一个基于 LimeMeta 的后端业务项目。业务项目只写自己的模型、接口和业务逻辑，框架能力通过 NuGet 包引用，不需要复制 LimeMeta 框架源码。

## 0. 框架能力清单

LimeMeta 当前覆盖这些后端能力：

- 自动建表：按模型同步数据库结构。
- 自动 GraphQL：模型自动生成查询、聚合、新增、修改、删除。
- REST API：基于 FastEndpoints 写普通 HTTP 接口。
- Logic 事件：模型查询、新增、修改、删除前后执行业务逻辑。
- 认证：内置用户、登录、JWT、AppKey。
- 权限：内置用户、角色、权限、部门、用户角色、部门角色。
- 文件：内置上传下载，支持本地存储和 123 云盘 CLI。
- WebSocket：HTTP 同端口统一入口，按消息类型分发。
- 种子数据：启动时加载 YAML 和 SQL 初始化数据。
- 配置：使用 YAML 管理端口、数据库、文件服务、WebSocket、日志等。

## 1. 项目结构

```text
LimeMetaService/
  LimeMetaService.sln
  NuGet.config
  README.md
  LimeMetaService/
    LimeMetaService.csproj
    Extensions.cs
    Models/
    Logics/
    TypeExtensions/
    Services/
    Configuration/
    Common/
  LimeMetaService.WebAPI/
    LimeMetaService.WebAPI.csproj
    Program.cs
    appsettings.yml
    appsettings.Development.yml
    Properties/
      launchSettings.json
    Seed/
      BeforeUpdateSchema.sql
      AfterUpdateSchema.sql
      system.yml
```

代码放置规则：

- `Models/`：数据库模型、DTO、业务实体。
- `Logics/`：模型事件逻辑，例如新增前校验、修改后同步、删除前拦截。
- `TypeExtensions/`：GraphQL 自定义 Query 和 Mutation。
- `Services/`：业务服务、第三方接口客户端、复杂业务编排。
- `Configuration/`：配置文件对应的强类型配置类。
- `Common/`：常量、工具类、共享类型。
- `Extensions.cs`：业务模块入口，统一注册服务、配置、GraphQL 扩展和 Logic。
- `LimeMetaService.WebAPI/`：启动项目，只放启动代码、配置文件、种子数据和发布相关内容。

推荐原则：

- 业务代码尽量放在 `LimeMetaService/` 项目里。
- `LimeMetaService.WebAPI/` 保持轻，只负责启动和配置。
- 不要把所有代码塞进 `Program.cs`。
- 不要修改 LimeMeta 框架包源码来写业务。

## 2. 运行项目

第一次使用私有包，需要配置 GitHub Packages 包源：

```powershell
dotnet nuget add source https://nuget.pkg.github.com/memsys-lizi/index.json `
  --name github-limemeta `
  --username memsys-lizi `
  --password <你的 GitHub PAT> `
  --store-password-in-clear-text
```

还原、构建、运行：

```powershell
dotnet restore
dotnet build
dotnet run --project LimeMetaService.WebAPI\LimeMetaService.WebAPI.csproj
```

默认 HTTP 地址：

```text
http://127.0.0.1:6675
```

GraphQL 地址：

```text
http://127.0.0.1:6675/api/gql
```

WebSocket 地址：

```text
ws://127.0.0.1:6675/api/ws
```

## 3. 端口在哪里配置

端口配置在：

```text
LimeMetaService.WebAPI/appsettings.yml
LimeMetaService.WebAPI/appsettings.Development.yml
```

字段是：

```yaml
Urls: "http://127.0.0.1:6675"
```

开发环境默认会读取：

```text
appsettings.yml
appsettings.Development.yml
```

后加载的 `appsettings.Development.yml` 会覆盖主配置。也就是说，开发时如果两个文件都写了 `Urls`，通常以 `appsettings.Development.yml` 为准。

模板里的 `launchSettings.json` 只设置：

```json
{
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

它不负责端口。端口统一看 YAML 里的 `Urls`。

常见写法：

```yaml
Urls: "http://127.0.0.1:6675"
```

只允许本机访问：

```yaml
Urls: "http://127.0.0.1:6675"
```

允许局域网或服务器外部访问：

```yaml
Urls: "http://*:6675"
```

Linux 服务器上通常写：

```yaml
Urls: "http://127.0.0.1:6675"
```

然后用 Nginx 或宝塔反向代理到这个端口。

## 4. YAML 配置字段说明

主配置文件：

```text
LimeMetaService.WebAPI/appsettings.yml
```

开发环境覆盖配置：

```text
LimeMetaService.WebAPI/appsettings.Development.yml
```

完整结构示例：

```yaml
Urls: "http://127.0.0.1:6675"

LimeMeta:
  ConnectionString: "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres"
  DataType: "PostgreSQL"
  FileStorePath: "./FileStore"
  FileStoreCount: 8192
  FileStore:
    Provider: "Local"
    Local:
      Path: "./FileStore"
      Count: 8192
    Pan123Cli:
      Command: "pan123"
      ParentFileId: 0
      UseDirectLink: true
      TempPath: "./TempUpload"
      Overwrite: false
  AdminPerm: "管理员"
  GuestPerm: "游客"
  AdminUserName: "admin"
  AdminUserPassword: "change-me-admin-password"
  DefaultUserPassword: "change-me-user-password"
  AutoSyncSchema: true
  LoadSeedOnStartup: true
  WebSocket:
    Path: "/api/ws"
    MaxMessageSize: 1048576

Serilog:
  MinimumLevel:
    Default: Information
    Override:
      Microsoft: Information
      Microsoft.AspNetCore: Information
  WriteTo:
    - Name: Console
    - Name: File
      Args:
        path: "Logs/error-.log"
        restrictedToMinimumLevel: Error
        rollingInterval: Day

AllowedHosts: "*"
```

字段含义：

| 字段 | 含义 |
| --- | --- |
| `Urls` | 应用监听地址和端口。VS 调试、命令行运行、Linux 部署都优先看这里。 |
| `LimeMeta.ConnectionString` | 数据库连接字符串。 |
| `LimeMeta.DataType` | FreeSql 数据库类型。当前模板默认 `PostgreSQL`。 |
| `LimeMeta.FileStorePath` | 旧版本地文件根目录配置，保留兼容。新项目优先用 `FileStore.Local.Path`。 |
| `LimeMeta.FileStoreCount` | 旧版本地文件分目录数量配置，保留兼容。新项目优先用 `FileStore.Local.Count`。 |
| `LimeMeta.FileStore.Provider` | 文件存储提供者，支持 `Local` 和 `Pan123Cli`。 |
| `LimeMeta.FileStore.Local.Path` | 本地文件保存根目录。 |
| `LimeMeta.FileStore.Local.Count` | 本地文件分目录数量。 |
| `LimeMeta.FileStore.Pan123Cli.Command` | 123 云盘 CLI 命令，可以是 `pan123`，也可以是绝对路径。 |
| `LimeMeta.FileStore.Pan123Cli.ParentFileId` | 上传到 123 云盘的父目录 ID。 |
| `LimeMeta.FileStore.Pan123Cli.UseDirectLink` | 下载时是否优先使用 123 云盘直链。 |
| `LimeMeta.FileStore.Pan123Cli.TempPath` | 使用 123 云盘上传时的本地临时目录。 |
| `LimeMeta.FileStore.Pan123Cli.Overwrite` | 上传同名文件时是否覆盖。 |
| `LimeMeta.AdminPerm` | 初始化管理员权限名。 |
| `LimeMeta.GuestPerm` | 初始化游客权限名。 |
| `LimeMeta.AdminUserName` | 初始化管理员用户名。 |
| `LimeMeta.AdminUserPassword` | 初始化管理员密码，生产环境必须修改。 |
| `LimeMeta.DefaultUserPassword` | 初始化普通用户默认密码，生产环境必须修改。 |
| `LimeMeta.AutoSyncSchema` | 启动时是否自动同步数据库表结构。开发环境可以开，生产环境谨慎。 |
| `LimeMeta.LoadSeedOnStartup` | 启动时是否加载 `Seed` 目录里的种子数据。 |
| `LimeMeta.WebSocket.Path` | WebSocket 统一入口路径，默认 `/api/ws`。 |
| `LimeMeta.WebSocket.MaxMessageSize` | WebSocket 单条消息最大字节数。 |
| `Serilog` | 日志配置。 |
| `AllowedHosts` | ASP.NET Core Host 限制，通常保持 `*`。 |

生产环境建议：

- 修改 `AdminUserPassword`。
- 修改 `DefaultUserPassword`。
- 修改 `JwtSignKey` 和 `Salt`，如果配置文件里显式写了这两个字段。
- `AutoSyncSchema` 是否开启要按项目情况决定。
- 数据库用户必须有建表权限。
- 文件目录、日志目录要确保运行用户有读写权限。

## 5. 新增模型

模型写在：

```text
LimeMetaService/Models/
```

每个模型需要一个 DTO。框架会用 DTO 做 GraphQL 新增输入和模型映射。

```csharp
namespace LimeMetaService.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Models;

[Table(Name = "decoration")]
public class Decoration : BaseAudit
{
    [Column(Name = "name", StringLength = 200)]
    public string Name { get; set; } = "";

    [Column(Name = "category", StringLength = 100)]
    public string Category { get; set; } = "";

    [Column(Name = "status", StringLength = 50)]
    public string Status { get; set; } = "Pending";
}

public class DecorationDto : BaseAuditDto
{
    public string Name { get; set; } = "";

    public string Category { get; set; } = "";

    public string Status { get; set; } = "Pending";
}
```

规则：

- 模型必须继承 `BaseObject`、`BaseAudit` 或 `BaseParentChildren`。
- 模型必须写 `[Table]`。
- 字段建议写 `[Column]`，明确数据库列名和长度。
- DTO 名称必须是 `模型名Dto`。
- DTO 和模型建议放在同一个命名空间。
- 启动时 `AutoSyncSchema: true` 会自动建表。

常用基类：

| 基类 | 适合场景 |
| --- | --- |
| `BaseObject` | 普通模型，带 `Id`、版本等基础字段。 |
| `BaseAudit` | 需要创建人、创建时间、修改人、修改时间的模型。 |
| `BaseParentChildren` | 树形结构模型，例如分类、部门。 |

## 6. 自动生成的 GraphQL

新增模型后，框架会自动生成：

```text
Decoration
DecorationAggr
insertDecoration
updateDecoration
deleteDecoration
```

命名规则：

- 查询字段：模型类名，例如 `Decoration`。
- 聚合字段：模型类名 + `Aggr`，例如 `DecorationAggr`。
- 新增 Mutation：`insert` + 模型类名。
- 修改 Mutation：`update` + 模型类名。
- 删除 Mutation：`delete` + 模型类名。

### 登录获取 token

大多数 GraphQL 查询和修改需要认证。先调用登录：

```graphql
mutation {
  login(username: "admin", password: "change-me-admin-password", code: null) {
    token
    name
  }
}
```

拿到 token 后，请求头带：

```text
Authorization: Bearer 你的token
```

### 自动查询

```graphql
query {
  Decoration(
    page: { index: 1, size: 20 }
    where: { status: { eq: "Pending" } }
    order: [{ name: ASC }]
  ) {
    total
    items {
      id
      name
      category
      status
      createTime
    }
  }
}
```

说明：

- `page`：分页参数。
- `where`：过滤条件，由 HotChocolate Filtering 提供。
- `order`：排序条件，由 HotChocolate Sorting 提供。
- `items`：当前页数据。
- `total`：总数量。

### 自动新增

```graphql
mutation {
  insertDecoration(
    objs: [
      {
        name: "现代吊灯"
        category: "灯具"
        status: "Pending"
      }
    ]
  )
}
```

返回值是新增对象的 `id` 列表。

### 自动修改

```graphql
mutation {
  updateDecoration(
    objs: [
      {
        id: "00000000-0000-0000-0000-000000000000"
        name: "现代吊灯 Pro"
        status: "Approved"
      }
    ]
  )
}
```

返回值是影响行数。

### 自动删除

```graphql
mutation {
  deleteDecoration(
    ids: [
      "00000000-0000-0000-0000-000000000000"
    ]
  )
}
```

返回值是影响行数。

### 自动聚合

聚合字段名是 `模型名Aggr`。实际可用字段取决于当前框架的聚合参数类型。

示例：

```graphql
query {
  DecorationAggr(
    fields: [
      { type: COUNT, name: "id" }
    ]
    groups: ["status"]
  )
}
```

如果聚合参数报错，请打开 `/api/gql` 的 schema 文档查看 `AggrField` 当前字段名，以运行时 schema 为准。

## 7. 新增 GraphQL 自定义 Query

自动 CRUD 解决普通增删改查。复杂统计、复杂业务查询，写到：

```text
LimeMetaService/TypeExtensions/
```

示例：

```csharp
namespace LimeMetaService.TypeExtensions;

using HotChocolate.Types;
using LimeMeta.Data;
using LimeMetaService.Models;

[ExtendObjectType("Query")]
public sealed class DecorationQueryExtensions
{
    public Task<long> pendingDecorationCount([Service] ILimeMeta meta)
    {
        return meta.Query<Decoration>()
            .Where(x => x.Status == "Pending")
            .CountAsync();
    }
}
```

然后在：

```text
LimeMetaService/Extensions.cs
```

注册：

```csharp
using LimeMetaService.TypeExtensions;

gqlBuilder.AddTypeExtension<DecorationQueryExtensions>();
```

调用：

```graphql
query {
  pendingDecorationCount
}
```

## 8. 新增 GraphQL 自定义 Mutation

业务动作、审核、批量处理等不适合直接暴露成通用 `updateXxx` 的操作，写自定义 Mutation。

示例：

```csharp
namespace LimeMetaService.TypeExtensions;

using HotChocolate;
using HotChocolate.Types;
using LimeMeta.Data;
using LimeMetaService.Models;

[ExtendObjectType("Mutation")]
public sealed class DecorationMutationExtensions
{
    public async Task<bool> approveDecoration(
        [Service] ILimeMeta meta,
        Guid id)
    {
        var obj = meta.Query<Decoration>()
            .Where(x => x.Id == id)
            .First();

        if (obj == null)
        {
            throw new GraphQLException("装修资源不存在");
        }

        obj.Status = "Approved";
        meta.Update(new[] { obj });
        return true;
    }
}
```

注册：

```csharp
using LimeMetaService.TypeExtensions;

gqlBuilder.AddTypeExtension<DecorationMutationExtensions>();
```

调用：

```graphql
mutation {
  approveDecoration(id: "00000000-0000-0000-0000-000000000000")
}
```

说明：

- 普通表数据新增、修改、删除优先使用自动生成的 Mutation。
- 有业务含义的动作，例如审核、发布、撤回、批处理，推荐写自定义 Mutation。
- 默认模板没有开启 GraphQL Subscription。实时通信建议使用框架内置 WebSocket。

## 9. 新增 REST API

REST API 适合：

- 文件回调。
- 第三方系统回调。
- 登录以外的特殊认证接口。
- 不适合放到 GraphQL 的简单 HTTP 接口。

推荐新建目录：

```text
LimeMetaService/Endpoints/
```

示例：

```csharp
namespace LimeMetaService.Endpoints;

using FastEndpoints;

public sealed class PingEndpoint : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/api/ping");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return SendAsync(new { ok = true }, cancellation: ct);
    }
}
```

带请求体的接口：

```csharp
namespace LimeMetaService.Endpoints;

using FastEndpoints;

public sealed class CreateDecorationRequest
{
    public string Name { get; set; } = "";
}

public sealed class CreateDecorationResponse
{
    public Guid Id { get; set; }
}

public sealed class CreateDecorationEndpoint
    : Endpoint<CreateDecorationRequest, CreateDecorationResponse>
{
    public override void Configure()
    {
        Post("/api/decorations");
    }

    public override async Task HandleAsync(CreateDecorationRequest req, CancellationToken ct)
    {
        await SendAsync(new CreateDecorationResponse
        {
            Id = Guid.NewGuid()
        }, cancellation: ct);
    }
}
```

框架已经注册了 FastEndpoints，业务项目被 WebAPI 引用后，Endpoint 会自动发现。

## 10. 新增 Logic

Logic 用来处理模型事件，写在：

```text
LimeMetaService/Logics/
```

示例：

```csharp
namespace LimeMetaService.Logics;

using LimeMeta.Logics;
using LimeMetaService.Models;
using Microsoft.Extensions.Logging;

public sealed class DecorationLogic : BaseLogic<Decoration>
{
    public DecorationLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeInsert += OnBeforeInsert;
        BeforeUpdate += OnBeforeUpdate;
    }

    private void OnBeforeInsert(object? sender, BeforeInsertEventArgs<Decoration> args)
    {
        foreach (var item in args.Objects)
        {
            item.Name = item.Name.Trim();
            item.Status = string.IsNullOrWhiteSpace(item.Status) ? "Pending" : item.Status;
        }
    }

    private void OnBeforeUpdate(object? sender, BeforeUpdateEventArgs<Decoration> args)
    {
        foreach (var item in args.Objects)
        {
            item["name"] = item["name"]?.ToString()?.Trim();
        }
    }
}
```

常见用途：

- 保存前补默认值。
- 修改前校验状态。
- 删除前判断是否允许删除。
- 查询前追加业务过滤。
- 修改后同步其他表。

模板里的 `Extensions.cs` 已经调用 `AddLimeMetaModule` 和 `UseLimeMetaModule`，新增 Logic 后不需要写反射注册。

## 11. 新增业务服务

服务类写在：

```text
LimeMetaService/Services/
```

示例：

```csharp
namespace LimeMetaService.Services;

public sealed class DecorationAuditService
{
    public bool CanApprove(string status)
    {
        return status == "Pending";
    }
}
```

在 `LimeMetaService/Extensions.cs` 注册：

```csharp
using LimeMetaService.Services;

services.AddScoped<DecorationAuditService>();
```

然后在 Endpoint、GraphQL 扩展或 Logic 中通过构造函数注入。

## 12. 新增业务配置

配置类写在：

```text
LimeMetaService/Configuration/
```

示例：

```csharp
namespace LimeMetaService.Configuration;

public sealed class DecorationOptions
{
    public int MaxUploadSizeMb { get; set; } = 100;

    public string ReviewMode { get; set; } = "Manual";
}
```

YAML：

```yaml
Decoration:
  MaxUploadSizeMb: 100
  ReviewMode: "Manual"
```

注册：

```csharp
using LimeMetaService.Configuration;

services.Configure<DecorationOptions>(configuration.GetSection("Decoration"));
```

使用：

```csharp
using Microsoft.Extensions.Options;

public sealed class DecorationAuditService(IOptions<DecorationOptions> options)
{
    private readonly DecorationOptions _options = options.Value;
}
```

## 13. 文件上传下载

框架内置文件接口：

```text
POST /api/file/upload
GET  /api/file/download
```

具体请求参数以当前 Swagger 或接口定义为准。

本地存储配置：

```yaml
LimeMeta:
  FileStore:
    Provider: "Local"
    Local:
      Path: "./FileStore"
      Count: 8192
```

123 云盘配置：

```yaml
LimeMeta:
  FileStore:
    Provider: "Pan123Cli"
    Pan123Cli:
      Command: "pan123"
      ParentFileId: 0
      UseDirectLink: true
      TempPath: "./TempUpload"
      Overwrite: false
```

说明：

- `Local` 会把文件保存到服务器本地目录。
- `Pan123Cli` 会调用服务器上已经安装和登录好的 123 云盘 CLI。
- LimeMeta 不保存 123 云盘账号密码。
- Linux 上要确保运行用户能执行 `pan123`，也能读写 `TempPath`。

## 14. WebSocket 开发

WebSocket 只有一个入口，默认：

```text
ws://127.0.0.1:6675/api/ws
```

不要为每个业务功能开一个 WebSocket 地址。业务分发靠消息 `type`。

推荐新建目录：

```text
LimeMetaService/WebSockets/
```

示例：

```csharp
namespace LimeMetaService.WebSockets;

using LimeMeta.WebSockets;

[WsController]
public sealed class NoticeWs
{
    [WsMessage("notice.ping")]
    public object Ping(LimeMetaWebSocketContext context)
    {
        return new
        {
            ok = true,
            connectionId = context.Connection.Id
        };
    }
}
```

客户端发送：

```json
{
  "type": "notice.ping",
  "data": {}
}
```

适合场景：

- 站内通知。
- 实时状态刷新。
- 审核结果推送。
- 长任务进度。

## 15. 种子数据

种子文件放在：

```text
LimeMetaService.WebAPI/Seed/
```

默认文件：

```text
BeforeUpdateSchema.sql
AfterUpdateSchema.sql
system.yml
```

含义：

- `BeforeUpdateSchema.sql`：同步表结构前执行。
- `AfterUpdateSchema.sql`：同步表结构后执行。
- `system.yml`：初始化数据。空数据可以写 `[]`。

是否启动加载由配置控制：

```yaml
LimeMeta:
  LoadSeedOnStartup: true
```

种子数据会按框架的数据加载逻辑写入数据库。涉及用户时，密码不要直接写明文业务密码，优先使用框架默认密码配置或专门的密码逻辑。

## 16. 发布部署

发布：

```powershell
dotnet publish LimeMetaService.WebAPI\LimeMetaService.WebAPI.csproj -c Release -o publish
```

发布产物在：

```text
publish/
```

Linux 运行：

```bash
cd /www/wwwroot/LimeMetaService/publish
dotnet LimeMetaService.WebAPI.dll
```

宝塔添加 .NET 项目时：

- 项目名称：自定义。
- 运行路径：`/www/wwwroot/LimeMetaService/publish`。
- 启动命令：`dotnet LimeMetaService.WebAPI.dll`。
- 项目端口：填写 `appsettings.yml` 里 `Urls` 的端口，例如 `6675`。
- 启动用户：要有配置文件、日志目录、文件目录的读写权限。

Nginx 反代到：

```text
http://127.0.0.1:6675
```

如果使用 WebSocket，反代需要支持 Upgrade 头。

## 17. 升级 LimeMeta 框架

修改：

```text
LimeMetaService/LimeMetaService.csproj
```

把版本改成新发布的版本：

```xml
<PackageReference Include="LimeMeta" Version="2026.621.1320" />
<PackageReference Include="LimeMeta.GraphQL" Version="2026.621.1320" />
```

然后执行：

```powershell
dotnet restore
dotnet build
```

如果 NuGet 缓存导致版本没有更新：

```powershell
dotnet nuget locals all --clear
dotnet restore
```

## 18. 常见问题

### 端口改了不生效

检查这两个文件：

```text
LimeMetaService.WebAPI/appsettings.yml
LimeMetaService.WebAPI/appsettings.Development.yml
```

开发环境下 `appsettings.Development.yml` 会覆盖 `appsettings.yml`。模板的 `launchSettings.json` 不配置端口。

### PostgreSQL 提示 permission denied for schema public

数据库用户缺少 `public` schema 建表权限。用管理员执行：

```sql
GRANT USAGE, CREATE ON SCHEMA public TO your_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO your_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO your_user;
```

### GitHub Packages restore 失败

通常是没有配置私有包源认证。重新执行：

```powershell
dotnet nuget remove source github-limemeta
dotnet nuget add source https://nuget.pkg.github.com/memsys-lizi/index.json `
  --name github-limemeta `
  --username memsys-lizi `
  --password <你的 GitHub PAT> `
  --store-password-in-clear-text
```

### 新模型没有出现在 GraphQL

检查：

- 模型是否继承 `BaseObject`、`BaseAudit` 或 `BaseParentChildren`。
- 模型是否写了 `[Table]`。
- 是否有 `模型名Dto`。
- 业务项目是否被 `WebAPI` 项目引用。
- `Extensions.cs` 是否保留了 `services.AddLimeMetaModule(typeof(Extensions).Assembly)`。
- `Program.cs` 是否调用了 `builder.Services.AddLimeMetaService(...)` 和 `app.UseLimeMetaService()`。

### 发布后找不到配置文件

检查 `LimeMetaService.WebAPI.csproj` 是否包含：

```xml
<Content Include="appsettings*.yml" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
<Content Include="Seed\**\*" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
```
