# LimeMetaService

这是一个基于 LimeMeta 的后端业务项目。项目只写自己的业务代码，框架能力通过 NuGet 包引用。

## 项目结构

```text
LimeMetaService/
  LimeMetaService.sln
  NuGet.config
  README.md
  LimeMetaService/
    Extensions.cs
    Models/
    Logics/
    TypeExtensions/
    Services/
    Configuration/
    Common/
  LimeMetaService.WebAPI/
    Program.cs
    appsettings.yml
    appsettings.Development.yml
    Seed/
```

代码放置规则：

- `Models/`：数据库模型、DTO、业务实体。
- `Logics/`：模型事件逻辑。
- `TypeExtensions/`：GraphQL 自定义查询和 Mutation。
- `Services/`：业务服务、第三方接口客户端。
- `Configuration/`：强类型配置类。
- `Common/`：常量、工具类、业务通用结构。
- `Extensions.cs`：业务模块入口，统一注册服务、配置、GraphQL 扩展和 Logic。
- `LimeMetaService.WebAPI/`：启动项目、配置文件、种子数据。

## 运行

先配置 GitHub Packages 私有源：

```powershell
dotnet nuget add source https://nuget.pkg.github.com/memsys-lizi/index.json `
  --name github-limemeta `
  --username memsys-lizi `
  --password <你的 GitHub PAT> `
  --store-password-in-clear-text
```

还原并运行：

```powershell
dotnet restore
dotnet run --project LimeMetaService.WebAPI\LimeMetaService.WebAPI.csproj
```

默认地址：

```text
http://127.0.0.1:6675
```

GraphQL：

```text
http://127.0.0.1:6675/api/gql
```

WebSocket：

```text
ws://127.0.0.1:6675/api/ws
```

## 新增模型

模型写在 `LimeMetaService/Models/`。每个模型需要一个 DTO。

```csharp
namespace LimeMetaService.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Models;

[Table(Name = "example_item")]
public class ExampleItem : BaseAudit
{
    [Column(Name = "name", StringLength = 200)]
    public string Name { get; set; } = "";
}

public class ExampleItemDto : BaseAuditDto
{
    public string Name { get; set; } = "";
}
```

启动时如果 `AutoSyncSchema: true`，框架会自动建表。GraphQL 也会自动生成查询和增删改。

## 新增 Logic

Logic 写在 `LimeMetaService/Logics/`。

```csharp
namespace LimeMetaService.Logics;

using LimeMeta.Logics;
using LimeMetaService.Models;
using Microsoft.Extensions.Logging;

public sealed class ExampleItemLogic : BaseLogic<ExampleItem>
{
    public ExampleItemLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeInsert += OnBeforeInsert;
    }

    private void OnBeforeInsert(object? sender, BeforeInsertEventArgs<ExampleItem> args)
    {
        foreach (var item in args.Objects)
        {
            item.Name = item.Name.Trim();
        }
    }
}
```

`Extensions.cs` 已经注册了当前业务程序集，不需要额外反射处理。

## 新增 REST API

推荐新建 `Endpoints/` 文件夹放 REST 接口。

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

## 新增 GraphQL 扩展

自动生成的 CRUD 不需要写代码。复杂查询写到 `TypeExtensions/`。

```csharp
namespace LimeMetaService.TypeExtensions;

using HotChocolate.Types;
using LimeMeta.Data;
using LimeMetaService.Models;

[ExtendObjectType("Query")]
public sealed class QueryExtensions
{
    public Task<long> exampleItemCount([Service] ILimeMeta meta)
    {
        return meta.Query<ExampleItem>().CountAsync();
    }
}
```

然后在 `LimeMetaService/Extensions.cs` 注册：

```csharp
gqlBuilder.AddTypeExtension<QueryExtensions>();
```

## 新增 WebSocket 消息

WebSocket 只有一个入口地址，业务靠消息类型分发。

```csharp
namespace LimeMetaService.WebSockets;

using LimeMeta.WebSockets;

[WsController]
public sealed class NoticeWs
{
    [WsMessage("notice.ping")]
    public object Ping(LimeMetaWebSocketContext context)
    {
        return new { ok = true, connectionId = context.Connection.Id };
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

## 配置

配置在 `LimeMetaService.WebAPI/appsettings.yml` 和 `appsettings.Development.yml`。

```yaml
Urls: "http://127.0.0.1:6675"

LimeMeta:
  ConnectionString: "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres"
  DataType: "PostgreSQL"
  AutoSyncSchema: true
  LoadSeedOnStartup: true
  FileStore:
    Provider: "Local"
  WebSocket:
    Path: "/api/ws"
```

生产环境建议：

- `AdminUserPassword` 改成强密码。
- `DefaultUserPassword` 改成强密码。
- `AutoSyncSchema` 是否开启按项目情况决定。
- 数据库用户需要有 schema 建表权限。

## 发布

```powershell
dotnet publish LimeMetaService.WebAPI\LimeMetaService.WebAPI.csproj -c Release -o publish
```

Linux 运行：

```bash
cd /www/wwwroot/LimeMetaService/publish
dotnet LimeMetaService.WebAPI.dll
```

宝塔添加 .NET 项目时：

- 运行路径填 `publish` 目录。
- 启动命令填 `dotnet LimeMetaService.WebAPI.dll`。
- 项目端口填 `appsettings.yml` 中 `Urls` 的端口。

## 升级框架

修改 `LimeMetaService/LimeMetaService.csproj`：

```xml
<PackageReference Include="LimeMeta" Version="2026.621.1320" />
<PackageReference Include="LimeMeta.GraphQL" Version="2026.621.1320" />
```

然后执行：

```powershell
dotnet restore
dotnet build
```
