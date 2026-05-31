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
├─ run.bat                # Windows 本地启动
└─ rel.bat                # NuGet 打包脚本
```

## 内置模块

核心模块：

- 用户：`User`
- 角色：`Role`
- 权限：`Perm`
- 用户角色：`UserRole`
- 部门：`Dept`
- 部门用户：`DeptUser`
- 部门角色：`DeptRole`
- AppKey：`AppKey`
- 文件信息：`FileInfo`

接口模块：

- GraphQL：`/api/gql`
- 文件上传：`POST /api/file/upload`
- 文件下载：`GET /api/file/download?id=文件ID`

上传文件会写入 `LimeMeta:FileStorePath` 指定的目录，并在 `file_info` 表中记录文件元数据。

## 配置文件

LimeMeta 使用 YAML 作为主配置格式。配置加载顺序在 `LimeMeta.WebAPI/Program.cs` 中定义：

1. `appsettings.yml`
2. `appsettings.{Environment}.yml`
3. 环境变量
4. 命令行参数

后加载的配置会覆盖先加载的配置。

### 主配置文件

主配置文件是：

```text
LimeMeta.WebAPI/appsettings.yml
```

生产部署主要改这个文件。完整示例：

```yaml
Urls: "http://127.0.0.1:8082"

LimeMeta:
  ConnectionString: "Host=127.0.0.1;Port=5432;Database=limemeta;Username=limemeta;Password=change-me"
  DataType: "PostgreSQL"
  FileStorePath: "/www/wwwroot/limemeta/FileStore"
  FileStoreCount: 8192

Serilog:
  MinimumLevel:
    Default: Information
    Override:
      Microsoft: Information
      Microsoft.AspNetCore: Information
  WriteTo:
    - Name: Console
      Args:
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
    - Name: File
      Args:
        path: "Logs/error-.log"
        restrictedToMinimumLevel: Error
        rollingInterval: Day
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

AllowedHosts: "*"
```

### 主配置含义

`Urls`

服务监听地址。线上建议使用：

```yaml
Urls: "http://127.0.0.1:8082"
```

这样服务只监听服务器本机端口，再由 Nginx 或宝塔反向代理到公网域名。

如果需要直接暴露端口，可以写：

```yaml
Urls: "http://0.0.0.0:8082"
```

开发环境也可以写：

```yaml
Urls: "http://*:8082"
```

`LimeMeta:ConnectionString`

数据库连接字符串。当前项目默认安装的是 PostgreSQL Provider，所以开箱使用 PostgreSQL。

PostgreSQL 示例：

```yaml
ConnectionString: "Host=127.0.0.1;Port=5432;Database=limemeta;Username=limemeta;Password=your-password"
```

`LimeMeta:DataType`

FreeSql 数据库类型。当前默认：

```yaml
DataType: "PostgreSQL"
```

如果要使用 MySQL，需要先在 `LimeMeta.WebAPI.csproj` 添加 MySQL Provider：

```xml
<PackageReference Include="FreeSql.Provider.MySql" Version="3.5.309" />
```

然后配置：

```yaml
ConnectionString: "Server=127.0.0.1;Port=3306;Database=limemeta;Uid=root;Pwd=your-password;Charset=utf8mb4;"
DataType: "MySql"
```

换数据库后需要实际验证建表、JSON 字段和索引行为，不能只改连接串就默认完全兼容。

`LimeMeta:FileStorePath`

上传文件保存根目录。

Linux 推荐使用绝对路径：

```yaml
FileStorePath: "/www/wwwroot/limemeta/FileStore"
```

如果写相对路径：

```yaml
FileStorePath: "./FileStore"
```

它会相对于程序运行目录保存。宝塔中通常就是项目运行路径。

上传后的实际文件路径格式：

```text
FileStorePath/存储编号/原文件名_GUID.扩展名
```

`LimeMeta:FileStoreCount`

每个文件存储子目录最多放多少个文件。默认：

```yaml
FileStoreCount: 8192
```

当一个子目录文件数量达到这个值后，会进入下一个编号目录。

`Serilog`

日志配置。当前配置了：

- 控制台输出：方便直接查看运行日志。
- 文件输出：错误日志写入 `Logs/error-.log`，按天滚动。

部署时如果运行目录是 `/www/wwwroot/limemeta`，日志目录就是：

```text
/www/wwwroot/limemeta/Logs
```

`AllowedHosts`

ASP.NET Core Host 过滤配置。默认：

```yaml
AllowedHosts: "*"
```

表示允许所有 Host。需要更严格时可以改成指定域名。

### 开发环境配置

开发环境配置文件：

```text
LimeMeta.WebAPI/appsettings.Development.yml
```

当环境是 `Development` 时，会覆盖 `appsettings.yml` 中的同名配置。

本地开发一般放本机数据库、本机文件目录：

```yaml
Urls: "http://*:8082"

LimeMeta:
  ConnectionString: "Host=localhost;Port=5432;Database=limemeta_dev;Username=postgres;Password=postgres"
  DataType: "PostgreSQL"
  FileStorePath: "./FileStore"
```

### IDE 调试配置

Visual Studio、Rider、VS Code 或 `dotnet run` 通常会读取：

```text
LimeMeta.WebAPI/Properties/launchSettings.json
```

它只用于本地开发调试。发布到 Linux 后，不要依赖这个文件。

当前配置：

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://*:8082",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## 本地运行

先确认本机 PostgreSQL 可用，并修改：

```text
LimeMeta.WebAPI/appsettings.Development.yml
```

然后在项目根目录执行：

```bash
dotnet run --project LimeMeta.WebAPI/LimeMeta.WebAPI.csproj
```

Windows 可以直接执行：

```bat
run.bat
```

访问：

```text
http://localhost:8082/api/gql
```

## 构建和发布

构建解决方案：

```bash
dotnet build LimeMeta.sln
```

发布 WebAPI：

```bash
dotnet publish LimeMeta.WebAPI/LimeMeta.WebAPI.csproj -c Release -o publish
```

发布后产物在：

```text
publish/
```

部署到 Linux 时，上传 `publish` 目录中的所有文件。

如果服务器没有安装 .NET Runtime，可以发布自包含版本：

```bash
dotnet publish LimeMeta.WebAPI/LimeMeta.WebAPI.csproj -c Release -r linux-x64 --self-contained true -o publish
```

自包含发布体积更大，但服务器不需要安装 .NET Runtime。

## Linux 部署

### 方式一：普通 Linux + systemd

服务器需要安装与项目版本匹配的 ASP.NET Core Runtime。当前项目是 `net10.0`。

检查运行环境：

```bash
dotnet --info
```

创建部署目录：

```bash
sudo mkdir -p /opt/limemeta
```

上传 `publish` 中的所有文件到：

```text
/opt/limemeta
```

编辑生产配置：

```bash
sudo nano /opt/limemeta/appsettings.yml
```

推荐生产配置：

```yaml
Urls: "http://127.0.0.1:8082"

LimeMeta:
  ConnectionString: "Host=127.0.0.1;Port=5432;Database=limemeta;Username=limemeta;Password=your-password"
  DataType: "PostgreSQL"
  FileStorePath: "/data/limemeta/files"
  FileStoreCount: 8192

Serilog:
  MinimumLevel:
    Default: Information
    Override:
      Microsoft: Information
      Microsoft.AspNetCore: Information
  WriteTo:
    - Name: Console
      Args:
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
    - Name: File
      Args:
        path: "Logs/error-.log"
        restrictedToMinimumLevel: Error
        rollingInterval: Day
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

AllowedHosts: "*"
```

创建上传目录和日志目录：

```bash
sudo mkdir -p /data/limemeta/files
sudo mkdir -p /opt/limemeta/Logs
```

如果使用 `www-data` 作为运行用户：

```bash
sudo chown -R www-data:www-data /data/limemeta
sudo chown -R www-data:www-data /opt/limemeta
```

临时运行测试：

```bash
cd /opt/limemeta
dotnet LimeMeta.WebAPI.dll
```

看到服务启动后，本机测试：

```bash
curl http://127.0.0.1:8082/api/gql
```

创建 systemd 服务：

```bash
sudo nano /etc/systemd/system/limemeta.service
```

写入：

```ini
[Unit]
Description=LimeMeta WebAPI
After=network.target

[Service]
WorkingDirectory=/opt/limemeta
ExecStart=/usr/bin/dotnet /opt/limemeta/LimeMeta.WebAPI.dll
Restart=always
RestartSec=5
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

启动服务：

```bash
sudo systemctl daemon-reload
sudo systemctl enable limemeta
sudo systemctl start limemeta
```

查看状态：

```bash
sudo systemctl status limemeta
```

查看日志：

```bash
sudo journalctl -u limemeta -f
```

Nginx 反向代理示例：

```nginx
server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://127.0.0.1:8082;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 方式二：宝塔 .NET 项目

本地发布：

```bash
dotnet publish LimeMeta.WebAPI/LimeMeta.WebAPI.csproj -c Release -o publish
```

上传 `publish` 中的所有文件到：

```text
/www/wwwroot/limemeta
```

修改服务器上的：

```text
/www/wwwroot/limemeta/appsettings.yml
```

推荐配置：

```yaml
Urls: "http://127.0.0.1:8082"

LimeMeta:
  ConnectionString: "Host=127.0.0.1;Port=5432;Database=limemeta;Username=limemeta;Password=your-password"
  DataType: "PostgreSQL"
  FileStorePath: "/www/wwwroot/limemeta/FileStore"
  FileStoreCount: 8192

Serilog:
  MinimumLevel:
    Default: Information
    Override:
      Microsoft: Information
      Microsoft.AspNetCore: Information
  WriteTo:
    - Name: Console
      Args:
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
    - Name: File
      Args:
        path: "Logs/error-.log"
        restrictedToMinimumLevel: Error
        rollingInterval: Day
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

AllowedHosts: "*"
```

创建目录并授权：

```bash
mkdir -p /www/wwwroot/limemeta/FileStore
mkdir -p /www/wwwroot/limemeta/Logs
chown -R www:www /www/wwwroot/limemeta
```

宝塔添加 `.Net项目` 时填写：

```text
项目名称：LimeMeta
运行路径：/www/wwwroot/limemeta
启动命令：dotnet LimeMeta.WebAPI.dll
项目端口：8082
.Net版本：选择服务器已安装的 .NET 10 / ASP.NET Core Runtime 10
开机启动：建议勾选
启动用户：www
项目备注：LimeMeta 后端服务
```

因为端口已经写在 `appsettings.yml` 的 `Urls` 中，所以启动命令不需要带 `--urls`。

宝塔网站反向代理：

```text
目标 URL：http://127.0.0.1:8082
```

如果使用域名 `api.example.com`，最终访问：

```text
https://api.example.com/api/gql
```

如果不做反向代理，且想直接访问服务器端口，需要把 `Urls` 改成：

```yaml
Urls: "http://0.0.0.0:8082"
```

并在宝塔安全中放行 `8082`。更推荐使用反向代理，不直接暴露后端端口。

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

约定：

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
using LimeMeta.Data;
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

