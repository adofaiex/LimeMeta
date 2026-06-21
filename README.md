# LimeMeta

LimeMeta 是一个模型驱动的 .NET 后端框架。它把数据库模型、自动建表、GraphQL 自动查询和修改、REST API、Logic 事件、认证、文件存储、WebSocket、种子数据加载组合在一起，适合拿来快速搭业务后端，也适合作为长期维护项目的基础框架。

这个仓库现在分成三类内容：

```text
LimeMeta/
  LimeMeta/                  核心框架库，发布为 NuGet 包 LimeMeta
  LimeMeta.GraphQL/          GraphQL 扩展库，发布为 NuGet 包 LimeMeta.GraphQL
  LimeMeta.WebAPI/           框架开发调试用启动项目，不作为业务模板源码
  templates/LimeMeta.Service PM 风格业务项目模板
  README.md                  框架维护、发布、使用说明
```

业务项目不应该复制框架源码。正确方式是：通过模板创建业务项目，业务项目只引用 `LimeMeta` 和 `LimeMeta.GraphQL` 包。以后框架更新，业务项目升级包版本即可。

## 核心能力

- 模型自动建表：模型带 `[Table]` 且继承 `BaseObject` 后，可由 FreeSql 同步数据库结构。
- GraphQL 自动查询：模型会自动生成查询字段，支持分页、过滤、排序、导航属性和聚合。
- GraphQL 自动修改：模型会自动生成新增、修改、删除 Mutation。
- REST API：基于 FastEndpoints，适合登录、上传、下载、回调等手写接口。
- Logic 事件：支持查询、新增、修改、删除前后执行业务逻辑。
- 业务模块注册：业务项目可以通过 `AddLimeMetaModule` / `UseLimeMetaModule` 明确注册自己的模型和 Logic。
- JWT 认证：支持 Bearer Token，也支持 AppKey 识别用户。
- 文件服务：内置上传下载接口，支持本地存储和 123 云盘 CLI 存储。
- WebSocket：HTTP 和 WebSocket 共用同一个端口，统一入口默认为 `/api/ws`，按消息类型分发。
- 种子数据：启动时可自动加载 `Seed` 目录中的初始化数据。
- 基础模型：内置用户、角色、权限、部门、文件、消息、AppKey 等基础模型。

## PM 风格业务项目结构

LimeMeta 的模板参考了 PM 项目的组织方式。一个业务项目生成后大概是这样：

```text
YourProject/
  YourProject.sln
  NuGet.config
  README.md
  YourProject/
    YourProject.csproj
    Extensions.cs
    Models/
    Logics/
    TypeExtensions/
    Services/
    Configuration/
    Common/
  YourProject.WebAPI/
    YourProject.WebAPI.csproj
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

目录含义：

- `Models/`：数据库实体、DTO、业务模型。
- `Logics/`：模型事件逻辑，例如插入前校验、修改后同步、删除前拦截。
- `TypeExtensions/`：GraphQL 自定义 Query 和 Mutation。
- `Services/`：业务服务、第三方平台客户端、复杂业务编排。
- `Configuration/`：配置文件对应的强类型配置。
- `Common/`：业务常量、工具类、共享小组件。
- `Extensions.cs`：业务模块入口，统一注册服务、配置、GraphQL 扩展和 Logic。
- `YourProject.WebAPI/`：启动项目，只负责宿主、配置、种子数据和发布。

## 发包、模板和升级

这一节专门记录 LimeMeta 框架包怎么发布，模板怎么安装、卸载、更新，以及业务项目怎么升级框架版本。

### GitHub Packages 包源

LimeMeta 默认发布到 GitHub Packages，包源地址是：

```xml
<add key="github-limemeta" value="https://nuget.pkg.github.com/memsys-lizi/index.json" />
```

第一次在一台电脑上还原 GitHub Packages 私有包，需要先配置包源认证：

```powershell
dotnet nuget add source https://nuget.pkg.github.com/memsys-lizi/index.json `
  --name github-limemeta `
  --username memsys-lizi `
  --password <你的 GitHub PAT> `
  --store-password-in-clear-text
```

GitHub PAT 至少需要 `read:packages` 权限。如果这台电脑还要发布包，还需要 `write:packages` 权限。

注意：

- `--store-password-in-clear-text` 会把 token 明文保存到本机 NuGet 配置里，通常在 `C:\Users\<用户名>\AppData\Roaming\NuGet\NuGet.Config`。
- 这是本机开发机可以接受的做法，但不要把带 token 的 `NuGet.Config` 提交到 git。
- token 如果发给别人或贴到公开位置，需要马上去 GitHub 删除并重新生成。

如果包源已经存在，重新配置前先删除旧源：

```powershell
dotnet nuget remove source github-limemeta
```

### 发布 LimeMeta 框架包

发布前进入框架仓库：

```powershell
cd C:\Users\lizi\Documents\Doc\.NET\LimeMeta
```

只打包，不推送：

```powershell
.\pack.bat
```

执行后会在 `.nuget/` 目录生成：

```text
LimeMeta.x.x.x.nupkg
LimeMeta.GraphQL.x.x.x.nupkg
```

推送到 GitHub Packages：

```powershell
$env:GITHUB_TOKEN="<你的 GitHub PAT>"
.\push-github-packages.bat
```

成功时会看到类似：

```text
正在将 LimeMeta.x.x.x.nupkg 推送到 ...
OK
已推送包。
```

发布规则：

- `LimeMeta` 会发布成 `LimeMeta.x.x.x.nupkg`。
- `LimeMeta.GraphQL` 会发布成 `LimeMeta.GraphQL.x.x.x.nupkg`。
- `LimeMeta.WebAPI` 是框架调试用启动项目，已经设置 `IsPackable=false`，不会被发布。
- 版本号由 `Directory.Build.props` 按当前时间生成，例如 `2026.621.1320`。
- 如果同一个版本已经发布过，GitHub Packages 不允许覆盖。需要重新打一个新版本，或者去 GitHub Packages 删除旧版本。

### 安装模板

模板用于创建新的业务项目。安装模板：

```powershell
dotnet new install C:\Users\lizi\Documents\Doc\.NET\LimeMeta
```

如果模板已经安装过，直接强制更新：

```powershell
dotnet new install C:\Users\lizi\Documents\Doc\.NET\LimeMeta --force
```

卸载模板：

```powershell
dotnet new uninstall C:\Users\lizi\Documents\Doc\.NET\LimeMeta
```

查看当前已安装模板：

```powershell
dotnet new list limemeta
```

### 创建业务项目

创建业务项目时，推荐指定明确的框架版本：

```powershell
dotnet new limemeta `
  -n LimeVoice `
  -o C:\Users\lizi\Documents\Doc\.NET\LimeVoice `
  --limeMetaVersion 2026.621.1243
```

如果不指定版本：

```powershell
dotnet new limemeta -n LimeVoice -o C:\Users\lizi\Documents\Doc\.NET\LimeVoice
```

默认 `--limeMetaVersion` 是 `*`，NuGet 会尝试还原包源里的最新版本。正式项目建议固定版本，避免某次 restore 悄悄升级框架。

创建后运行：

```powershell
cd C:\Users\lizi\Documents\Doc\.NET\LimeVoice
dotnet restore
dotnet build
```

### 升级业务项目里的 LimeMeta

业务项目升级框架时，不需要复制源码，也不需要重新创建项目。只改业务项目 `.csproj` 里的包版本。

例如打开：

```text
C:\Users\lizi\Documents\Doc\.NET\LimeVoice\LimeVoice\LimeVoice.csproj
```

把版本改成新发布的版本：

```xml
<PackageReference Include="LimeMeta" Version="2026.621.1243" />
<PackageReference Include="LimeMeta.GraphQL" Version="2026.621.1243" />
```

然后执行：

```powershell
dotnet restore
dotnet build
```

如果 NuGet 缓存导致还是旧包，可以清理缓存：

```powershell
dotnet nuget locals all --clear
dotnet restore
```

### 模板更新和框架包更新的区别

这两个东西不要混：

- 发布 `LimeMeta` / `LimeMeta.GraphQL` 包：影响业务项目引用到的框架代码。
- 安装或更新 `limemeta` 模板：只影响以后新建项目时生成的文件结构。
- 已经创建出来的业务项目，不会因为模板更新而自动改变文件结构。
- 已经创建出来的业务项目，要升级框架能力，改 `.csproj` 里的包版本。

通常流程是：

```text
改 LimeMeta 框架源码
运行 .\pack.bat
运行 .\push-github-packages.bat
业务项目修改 PackageReference 版本
业务项目 dotnet restore
业务项目 dotnet build
```

如果模板本身也改了：

```text
改 templates/LimeMeta.Service
dotnet new install C:\Users\lizi\Documents\Doc\.NET\LimeMeta --force
以后新项目用新模板创建
```

## 业务项目怎么开发

### 新增模型

模型放在业务项目的 `Models/`。

```csharp
namespace LimeVoice.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Models;

[Table(Name = "beatmap")]
public class Beatmap : BaseAudit
{
    [Column(Name = "title", StringLength = 200)]
    public string Title { get; set; } = "";

    [Column(Name = "artist", StringLength = 200)]
    public string Artist { get; set; } = "";

    [Column(Name = "status")]
    public string Status { get; set; } = "Pending";
}

public class BeatmapDto : BaseAuditDto
{
    public string Title { get; set; } = "";

    public string Artist { get; set; } = "";

    public string Status { get; set; } = "Pending";
}
```

规则：

- 表模型继承 `BaseObject`、`BaseAudit` 或 `BaseParentChildren`。
- 模型要写 `[Table]`。
- 字段建议写 `[Column]`，明确数据库列名。
- 每个模型都要有同命名空间 DTO，例如 `Beatmap` 对应 `BeatmapDto`。
- `AutoSyncSchema: true` 时，启动会自动同步表结构。

### 新增 Logic

Logic 放在 `Logics/`。

```csharp
namespace LimeVoice.Logics;

using LimeMeta.Logics;
using LimeVoice.Models;
using Microsoft.Extensions.Logging;

public sealed class BeatmapLogic : BaseLogic<Beatmap>
{
    public BeatmapLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeInsert += OnBeforeInsert;
    }

    private void OnBeforeInsert(object? sender, BeforeInsertEventArgs<Beatmap> args)
    {
        foreach (var item in args.Objects)
        {
            item.Status = string.IsNullOrWhiteSpace(item.Status) ? "Pending" : item.Status;
        }
    }
}
```

模板里的 `Extensions.cs` 已经注册了当前业务程序集，新增的 Logic 会被框架发现。

### 新增 REST API

REST API 可以放在业务项目的 `Services/` 配套，也可以单独建 `Endpoints/` 文件夹。推荐业务接口放在业务项目，不放在 `WebAPI` 启动项目。

```csharp
namespace LimeVoice.Endpoints;

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

`LimeMeta` 已经在框架里注册了 FastEndpoints，业务项目只要被启动项目引用，Endpoint 会被发现。

### 新增 GraphQL 扩展

自动 CRUD 不需要手写。只有复杂查询、统计、业务 Mutation 才需要放到 `TypeExtensions/`。

```csharp
namespace LimeVoice.TypeExtensions;

using HotChocolate.Types;
using LimeMeta.Data;
using LimeVoice.Models;

[ExtendObjectType("Query")]
public sealed class QueryExtensions
{
    public Task<long> pendingBeatmapCount([Service] ILimeMeta meta)
    {
        return meta.Query<Beatmap>().Where(x => x.Status == "Pending").CountAsync();
    }
}
```

然后在业务项目 `Extensions.cs` 里注册：

```csharp
gqlBuilder.AddTypeExtension<QueryExtensions>();
```

Mutation 同理：

```csharp
[ExtendObjectType("Mutation")]
public sealed class MutationTypeExtension
{
}
```

### 新增 WebSocket 消息

WebSocket 共用 HTTP 端口，不需要单独开端口。默认地址是：

```text
ws://127.0.0.1:6675/api/ws
```

业务消息处理类可以放在 `Services/` 或单独建 `WebSockets/`。

```csharp
namespace LimeVoice.WebSockets;

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

客户端发消息：

```json
{
  "type": "notice.ping",
  "data": {}
}
```

## 配置文件

主配置是 `YourProject.WebAPI/appsettings.yml`，开发环境覆盖配置是 `appsettings.Development.yml`。

常用配置：

```yaml
Urls: "http://127.0.0.1:6675"

LimeMeta:
  ConnectionString: "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres"
  DataType: "PostgreSQL"
  AutoSyncSchema: true
  LoadSeedOnStartup: true
  AdminUserName: "admin"
  AdminUserPassword: "change-me-admin-password"
  DefaultUserPassword: "change-me-user-password"
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
  WebSocket:
    Path: "/api/ws"
    MaxMessageSize: 1048576
```

说明：

- `Urls`：应用监听地址和端口。发布环境没有 VS 的 `launchSettings.json`，所以生产端口看这里。
- `ConnectionString`：数据库连接字符串。
- `DataType`：FreeSql 数据库类型，常用 `PostgreSQL` 或 `MySql`。
- `AutoSyncSchema`：启动时是否同步表结构。开发环境可以开，生产环境建议谨慎。
- `LoadSeedOnStartup`：启动时是否加载种子数据。
- `FileStore.Provider`：`Local` 或 `Pan123Cli`。
- `WebSocket.Path`：WebSocket 统一入口。

## 数据库选择

LimeMeta 底层使用 FreeSql，不是只能使用 PostgreSQL。当前框架对 PostgreSQL 做了更多优化，MySQL 也可以用于普通业务模型、自动建表、GraphQL、Logic、文件、WebSocket 等常规能力。

PostgreSQL 推荐配置：

```yaml
LimeMeta:
  DataType: "PostgreSQL"
  ConnectionString: "Host=127.0.0.1;Port=5432;Database=app;Username=postgres;Password=postgres"
```

MySQL 推荐配置：

```yaml
LimeMeta:
  DataType: "MySql"
  ConnectionString: "Server=127.0.0.1;Port=3306;Database=app;Uid=root;Pwd=your_password;Charset=utf8mb4;"
```

数据库 Provider：

- 框架调试 WebAPI 和项目模板已经同时引用 `FreeSql.Provider.PostgreSQL` 和 `FreeSql.Provider.MySqlConnector`。
- 如果业务项目删除了某个 Provider 包，对应数据库就不能使用。
- MySQL、MariaDB、Percona、Aurora、TiDB 等 MySQL 兼容数据库优先使用 `FreeSql.Provider.MySqlConnector`。

兼容差异：

| 能力 | PostgreSQL | MySQL |
| --- | --- | --- |
| 普通模型自动建表 | 支持 | 支持 |
| 自动 GraphQL 查询和修改 | 支持 | 支持 |
| Logic 事件 | 支持 | 支持 |
| 用户、角色、权限、部门 | 支持 | 支持 |
| 文件上传下载、123 云盘、WebSocket | 支持 | 支持 |
| 普通 `[Indexed]` 索引 | 支持 | 支持 |
| `JsonElement` 字段保存 | 支持 | 基础支持，建议按项目测试 |
| `JsonElement` GIN 索引 | 支持 | 不启用 |
| Geometry 空间字段 | 优先支持 | 暂不承诺完整支持 |

注意：从 PostgreSQL 切换到 MySQL 不是无痛迁移。通常需要新建 MySQL 数据库，让框架重新建表，再做数据迁移。

## 文件存储

本地存储：

```yaml
LimeMeta:
  FileStore:
    Provider: "Local"
    Local:
      Path: "./FileStore"
      Count: 8192
```

123 云盘 CLI：

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

服务器需要提前安装并登录配置好 123 云盘 CLI。LimeMeta 只负责调用 CLI，不在配置文件里保存云盘账号密码。

## 运行和部署

本地运行：

```powershell
cd YourProject\YourProject.WebAPI
dotnet run
```

发布：

```powershell
dotnet publish YourProject.WebAPI\YourProject.WebAPI.csproj -c Release -o publish
```

Linux 运行：

```bash
cd /www/wwwroot/YourProject/publish
dotnet YourProject.WebAPI.dll
```

宝塔部署时：

- 项目名称：自定义。
- 运行路径：`/www/wwwroot/YourProject/publish`。
- 启动命令：`dotnet YourProject.WebAPI.dll`。
- 项目端口：填写 `appsettings.yml` 里的端口，例如 `6675`。
- Net 版本：选择服务器安装的 .NET 版本。
- 生产端口优先写在 `appsettings.yml` 的 `Urls`。

如果用 Nginx 反代，反代到：

```text
http://127.0.0.1:6675
```

WebSocket 也走同一个端口和同一个站点反代，需要允许 Upgrade 头。

## 常见问题

### PostgreSQL 提示 permission denied for schema public

宝塔新建 PostgreSQL 数据库时，用户可能只有数据库权限，没有 `public` schema 建表权限。需要用管理员账号执行：

```sql
GRANT USAGE, CREATE ON SCHEMA public TO your_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO your_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO your_user;
```

### MySQL 怎么配置

MySQL 连接字符串示例：

```yaml
LimeMeta:
  DataType: "MySql"
  ConnectionString: "Server=127.0.0.1;Port=3306;Database=app;Uid=app_user;Pwd=your_password;Charset=utf8mb4;"
```

MySQL 需要先创建数据库，并给用户建表权限：

```sql
CREATE DATABASE app CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'app_user'@'%' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON app.* TO 'app_user'@'%';
FLUSH PRIVILEGES;
```

如果提示找不到 MySQL Provider，检查 WebAPI 项目是否引用了 `FreeSql.Provider.MySqlConnector`。

### publish 里没有配置文件

检查 `WebAPI.csproj` 是否有：

```xml
<Content Include="appsettings*.yml" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
<Content Include="Seed\**\*" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
```

### VS 调试端口和生产端口谁生效

模板里的 `launchSettings.json` 不写端口。开发和生产都优先看 `appsettings*.yml` 的 `Urls`。

### GitHub Packages 还原失败

一般是没有配置包源认证，或者 PAT 没有私有包读取权限。重新执行 `dotnet nuget add source`，确认 `NuGet.config` 中有 `github-limemeta`。
