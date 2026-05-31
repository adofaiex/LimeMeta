# LimeMeta

LimeMeta 是一个基于模型驱动的 .NET 后端框架。它把实体模型、数据库表结构、GraphQL 查询、通用增删改、业务事件和种子数据加载组合在一起，适合快速搭建可长期维护的业务后端。

## 核心能力

- 模型自动建表：启动时扫描所有带 `[Table]` 且继承 `BaseObject` 的模型，并通过 FreeSql 同步表结构。
- GraphQL 自动查询：每个模型会自动生成同名查询字段，支持分页、过滤、排序和导航属性加载。
- GraphQL 自动变更：每个模型会自动生成 `insertXxx`、`updateXxx`、`deleteXxx`。
- Logic 事件机制：查询、新增、更新、删除前后都能挂业务逻辑。
- JWT 认证：支持 Bearer Token，也支持 AppKey 换取用户身份。
- 用户、角色、权限、部门基础模型：内置 RBAC 相关表结构。
- 文件上传下载：内置 `/api/file/upload` 和 `/api/file/download`。
- 种子数据：启动时加载 `LimeMeta.WebAPI/Seed/*.yaml`。

## 项目结构

```text
LimeMeta/
├─ LimeMeta/              # 核心框架：模型、数据访问、Logic、FastEndpoints 文件接口
├─ LimeMeta.GraphQL/      # GraphQL 查询、Mutation 自动注册
├─ LimeMeta.WebAPI/       # Web 启动项目、配置、种子数据
├─ LimeMeta.sln           # 解决方案
├─ run.bat                # 本地启动
└─ rel.bat                # NuGet 打包脚本
```

## 启动

先修改 `LimeMeta.WebAPI/appsettings.Development.yml` 中的 PostgreSQL 连接字符串，然后执行：

```bash
dotnet run --project LimeMeta.WebAPI/LimeMeta.WebAPI.csproj
```

默认 GraphQL 地址：

```text
/api/gql
```

开发环境会开启 Swagger。

## 配置

主要配置节：

```yaml
Urls: "http://127.0.0.1:8082"

LimeMeta:
  ConnectionString: "Host=localhost;Port=5432;Database=limemeta_dev;Username=postgres;Password=postgres"
  DataType: "PostgreSQL"
  FileStorePath: "./FileStore"
  FileStoreCount: 8192
```

`Urls` 是服务监听地址。开发环境可以使用 `http://*:8082`，线上建议使用 `http://127.0.0.1:8082`，再由 Nginx 或宝塔反向代理到公网域名。

生产环境不要把真实密码、密钥、连接字符串提交进仓库，优先使用环境变量或部署平台的密钥配置。

## 新增业务模型

新增模型时，推荐放在业务模块的 `Models` 目录。一个模型至少包含实体和 DTO：

```csharp
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using LimeMeta.Models;

namespace LimeMeta.Models;

[Table(Name = "project")]
public class Project : BaseAudit
{
    [Column(Name = "name"), Indexed]
    public required string Name { get; set; }

    [Column(Name = "code"), Indexed]
    public string? Code { get; set; }
}

public class ProjectDto : BaseDto
{
    public required string Name { get; set; }

    public string? Code { get; set; }
}
```

约定很重要：

- 实体必须继承 `BaseObject` 或它的子类。
- 实体必须有 `[Table(Name = "...")]`。
- DTO 名称必须是 `实体名 + Dto`，例如 `Project` 对应 `ProjectDto`。
- DTO 必须和实体在同一个程序集内，否则自动 Mutation 找不到 DTO。

完成后启动服务，框架会自动同步表结构，并生成：

```text
Project
insertProject
updateProject
deleteProject
```

## 字段暴露规则

实体字段决定数据库结构，DTO 决定自动新增接口可传哪些字段。

敏感字段不要放进 DTO。例如密码、密钥、Token、内部状态：

```csharp
public class ProjectDto : BaseDto
{
    public required string Name { get; set; }
}
```

如果字段不能被查询暴露，需要结合 GraphQL 类型配置或专门的输出 DTO 处理。不要只依赖前端不查这个字段。

当前自动 `updateXxx` 接收 JSON，因此敏感字段还需要在 Logic 中兜底拦截：

```csharp
public sealed class ProjectLogic : BaseLogic<Project>
{
    public ProjectLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeUpdate += OnBeforeUpdate;
    }

    private void OnBeforeUpdate(object? sender, BeforeUpdateEventArgs<Project> e)
    {
        foreach (var (oldObj, newObj) in e.Objs)
        {
            if (oldObj.Code != newObj.Code)
            {
                throw new Exception("不允许通过通用更新接口修改内部编码");
            }
        }
    }
}
```

密码、密钥这类字段建议只通过专门接口修改，不走自动 CRUD。

## 新增业务逻辑

Logic 用来处理校验、默认值、级联操作、权限过滤、外部系统通知等业务规则。

```csharp
public sealed class ProjectLogic : BaseLogic<Project>
{
    public ProjectLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeInsert += OnBeforeInsert;
        BeforeDelete += OnBeforeDelete;
    }

    private void OnBeforeInsert(object? sender, BeforeInsertEventArgs<Project> e)
    {
        foreach (var obj in e.Objs)
        {
            if (string.IsNullOrWhiteSpace(obj.Name))
            {
                throw new Exception("项目名称不能为空");
            }
        }
    }

    private void OnBeforeDelete(object? sender, BeforeDeleteEventArgs<Project> e)
    {
        // 删除前做关联检查或级联处理
    }
}
```

Logic 会被框架自动扫描。执行顺序由 `Order` 控制，数值越小越先执行。

## 新增 GraphQL 接口

普通 CRUD 不需要手写接口。需要特殊动作时，可以扩展 Query 或 Mutation。

```csharp
using HotChocolate.Types;
using LimeMeta.GraphQL;

namespace LimeMeta.ProjectModule;

[ExtendObjectType(typeof(Mutation))]
public class ProjectMutationExtensions
{
    public bool ArchiveProject(Guid id, [Service] ILimeMeta meta)
    {
        var project = meta.Query<Project>().FirstOrDefault(x => x.Id == id);
        if (project == null)
        {
            throw new GraphQLException("项目不存在");
        }

        // 修改业务状态
        meta.Update(new[] { project });
        return true;
    }
}
```

如果新扩展类型放在独立项目中，需要在 GraphQL 注册阶段调用 `AddTypeExtension<T>()`。

## 新增 REST 接口

REST 接口使用 FastEndpoints：

```csharp
using FastEndpoints;

public class PingEndpoint : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/api/ping");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.OkAsync("pong", ct);
    }
}
```

REST 接口中需要访问数据库时，优先使用 `ILimeMeta`，不要直接绕过框架操作数据库，否则会跳过 Logic 事件。

## 种子数据

种子文件放在：

```text
LimeMeta.WebAPI/Seed
```

文件名必须和模型类名一致，例如：

```text
Project.yaml
```

启动时框架会读取种子文件，并根据文件修改时间和对象 `Ver` 判断是否需要写入或更新。

## 数据访问

常用方式：

```csharp
var query = meta.Query<Project>().Where(x => x.Name.Contains("A"));
var page = meta.Select(query, new PageModel { Index = 1, Size = 20 }, userId: userId);
```

注意：

- `meta.Query<T>()` 只是查询入口，不触发查询前后 Logic。
- `meta.Select(...)` 会触发 `BeforeSelect` 和 `AfterSelect`。
- `meta.Insert(...)`、`meta.Update(...)`、`meta.Delete(...)` 会触发对应 Logic。

## 内置依赖

主要 NuGet 包：

- FreeSql：ORM 和表结构同步。
- HotChocolate：GraphQL 服务。
- FastEndpoints：REST Endpoint。
- Serilog：日志。
- YamlDotNet 与 NetEscapades.Configuration.Yaml：YAML 解析和配置。
- AutoMapper：DTO 到实体的映射。

AutoMapper 的新版本存在授权要求，商业项目使用前需要确认许可证。也可以替换为手写映射、Mapperly 或其他符合授权要求的映射方案。

## 开发约定

- 一个业务模块保持清晰目录结构：`Models`、`Logics`、`Endpoints`、`GraphQL`。
- 普通表优先通过模型生成 CRUD。
- 复杂业务动作写专用 Mutation 或 Endpoint。
- 敏感字段必须同时处理查询暴露、DTO 入参、更新拦截。
- 业务规则放在 Logic，不要散落在前端或多个接口里。
- 配置和密钥不要提交真实值。
