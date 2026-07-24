# 配置、种子、文件存储与部署

## 配置从哪里来

模板按以下顺序加载配置，后面的值覆盖前面的值：

```text
appsettings.yml
→ appsettings.<Environment>.yml
→ 环境变量
→ 命令行参数
```

环境由 `ASPNETCORE_ENVIRONMENT` 或 `DOTNET_ENVIRONMENT` 决定；都未设置时按 `Production` 启动。

YAML：

```yaml
LimeMeta:
  ConnectionString: ""
```

对应环境变量：

```text
LimeMeta__ConnectionString
```

双下划线 `__` 表示配置层级。容器、CI 和生产主机应通过秘密管理系统注入连接串、管理员初始密码和 JWT 密钥，不要把它们提交进仓库。

## LimeMeta 配置说明

```yaml
LimeMeta:
  ConnectionString: ""
  DataType: "MySql"

  AdminPerm: "管理员"
  GuestPerm: "游客"
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

| 配置 | 含义 |
| --- | --- |
| `ConnectionString` | FreeSql 数据库连接串 |
| `DataType` | `MySql` 或 `PostgreSQL` |
| `AdminPerm` | 被视为管理员权限的名称 |
| `GuestPerm` | 预留的游客权限名称；不会自动创建游客数据 |
| `AdminUserName` | 启动时确保存在的管理员用户名 |
| `AdminUserPassword` | 只用于首次创建管理员，不会覆盖已有密码 |
| `JwtSignKey` | JWT 对称签名密钥 |
| `JwtExpires` | JWT 有效期，单位毫秒 |
| `AutoSyncSchema` | 启动时是否根据模型同步表结构 |
| `LoadSeedOnStartup` | 启动时是否初始化管理员并加载 Seed |
| `FileStore.Provider` | 当前上传使用的文件 Provider |
| `WebSocket.Path` | WebSocket 统一入口 |
| `WebSocket.MaxMessageSize` | 单条 WebSocket 消息最大字节数 |

模板仍保留 `FileStorePath`、`FileStoreCount` 作为旧配置兼容项；新项目优先使用 `FileStore.Local.Path` 和 `FileStore.Local.Count`。

## 启动配置验证

所有环境都要求：

- 数据库连接串不为空。
- JWT 密钥至少 32 个 UTF-8 字节。
- 管理员用户名不为空。
- 管理员初始密码不为空。

非 Development 环境还要求：

- 连接串和 JWT 密钥不能包含明显示例标记。
- 管理员密码不能是 `admin`、`password`、`change-me` 等示例值。
- 管理员密码至少 12 个字符。

验证失败时应用会在连接数据库前拒绝启动。Development 允许明显标记的示例值，只是为了降低首次运行门槛。

## MySQL：模板默认数据库

创建数据库：

```sql
CREATE DATABASE limemeta_service
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
```

建议为应用创建单独账号，而不是在生产使用 `root`：

```sql
CREATE USER 'limemeta_app'@'%' IDENTIFIED BY 'replace-with-a-strong-password';
GRANT ALL PRIVILEGES ON limemeta_service.* TO 'limemeta_app'@'%';
FLUSH PRIVILEGES;
```

开发连接串：

```yaml
LimeMeta:
  DataType: "MySql"
  ConnectionString: "Server=127.0.0.1;Port=3306;Database=limemeta_service;Uid=limemeta_app;Pwd=replace-with-a-strong-password;Charset=utf8mb4;"
```

框架使用 `FreeSql.Provider.MySqlConnector`。连接失败时先检查：

- MySQL 是否监听 `127.0.0.1:3306`。
- 数据库是否已创建。
- 用户授权的 Host 是否与连接来源匹配。
- 连接串中的 `Uid`、`Pwd` 和数据库名。
- 应用是否实际运行在容器中；容器内的 `127.0.0.1` 不是宿主机。

## 切换 PostgreSQL

模板已引用 PostgreSQL Provider，只需修改：

```yaml
LimeMeta:
  DataType: "PostgreSQL"
  ConnectionString: "Host=127.0.0.1;Port=5432;Database=limemeta_service;Username=limemeta_app;Password=replace-with-a-strong-password"
```

切换数据库不是对现有数据的自动迁移。应新建目标数据库、验证表结构，并使用专门的数据迁移流程。

## 表结构同步和 SQL 顺序

当 `AutoSyncSchema: true` 时，启动顺序是：

```text
Seed/BeforeUpdateSchema.sql
→ 对所有发现的模型执行 FreeSql SyncStructure
→ Seed/AfterUpdateSchema.sql
```

这两个 SQL 文件即使暂时为空也要保留；当前实现会直接读取它们。

适用方式：

- `BeforeUpdateSchema.sql`：同步前需要执行的兼容 SQL。
- `AfterUpdateSchema.sql`：索引、视图、数据库特有对象或同步后修复。

自动同步适合开发和受控的小型部署。生产数据库变更应：

1. 先备份。
2. 在与生产结构一致的测试库验证。
3. 评估删列、改类型、长事务和锁表风险。
4. 高风险项目关闭 `AutoSyncSchema`，改为审核过的数据库迁移。

## YAML 种子规则

文件必须放在 WebAPI 的 `Seed/`，并准确命名为：

```text
<ModelType>.yaml
```

例如：

```text
Seed/Article.yaml
Seed/Category.yaml
Seed/Perm.yaml
```

`system.yml`、`articles.yml` 等名称不会被加载。

`Seed/Category.yaml`：

```yaml
- id: "20000000-0000-0000-0000-000000000001"
  name: "技术"
  sn: 10
  parentId:

- id: "20000000-0000-0000-0000-000000000002"
  name: "后端"
  sn: 20
  parentId: "20000000-0000-0000-0000-000000000001"
```

规则：

- YAML 顶层是对象数组。
- 属性使用 camelCase，例如 `parentId`。
- 为需要跨环境引用的数据使用固定 `Guid`。
- 关联模型 Seed 引用这些固定 ID。
- 文件会随构建和发布复制到输出目录。

加载时，框架用 ID 查找已有对象：

- ID 不存在：插入。
- ID 存在且该行 `Ver` 早于种子文件最后修改时间：更新。
- 数据已是新版本：跳过。

种子文件是“按稳定 ID 合并”，不是清空表后重建。它也不会删除已经从 YAML 移除的数据库行。

修改 Seed 后应真正更新文件修改时间。若部署系统在打包/解包时重写时间戳，应在目标环境验证版本判断是否符合预期。

所有模型 Seed 都以管理员用户 ID 执行，因而 `BaseAudit` 会记录管理员为创建者/修改者。

## 种子依赖顺序

框架按发现的模型顺序加载 Seed，这个顺序不应被当成稳定的业务契约。有关联依赖时：

- 使用数据库允许暂时为空的外键设计；或
- 在 `AfterUpdateSchema.sql` 中处理数据库级固定数据；或
- 写明确的应用初始化服务；或
- 确保 Seed 不依赖导航对象，只依赖固定外键 ID，并做集成测试。

不要依赖文件名排序控制加载顺序。

## 文件上传和下载

内置接口：

```text
POST /api/file/upload
GET  /api/file/download?id=<FileInfo ID>
```

它们默认需要认证。上传使用 `multipart/form-data`，字段名为 `Files`，可一次上传多个文件：

```bash
curl -X POST "http://127.0.0.1:6675/api/file/upload" \
  -H "Authorization: Bearer $TOKEN" \
  -F "Files=@./report.pdf" \
  -F "Files=@./image.png"
```

响应示例：

```json
{
  "items": [
    {
      "id": "文件元数据 ID",
      "name": "report.pdf",
      "provider": "Local",
      "providerId": null
    }
  ]
}
```

下载：

```bash
curl -L \
  -H "Authorization: Bearer $TOKEN" \
  "http://127.0.0.1:6675/api/file/download?id=<文件元数据 ID>" \
  --output report.pdf
```

上传成功后框架会创建 `FileInfo`。删除 `FileInfo` 时，`FileInfoLogic` 会调用相应 Provider 的 `DeleteAsync`：

- Local 会删除本地物理文件。
- 当前 Pan123Cli 实现只删除数据库记录，不会删除云盘文件。

文件接口目前没有内置扩展名、MIME、病毒扫描和单文件大小白名单。`Program.cs` 允许的请求体上限很大；生产项目必须根据业务风险增加限制和扫描。

## Local 文件存储

```yaml
LimeMeta:
  FileStore:
    Provider: "Local"
    Local:
      Path: "/var/lib/limemeta/files"
      Count: 8192
```

`Count` 是每个子目录最多保存的文件元数据数量。应用进程必须对目录有创建、写入、读取和删除权限。

容器部署时要把该目录挂载到持久卷，否则容器重建会丢失物理文件，但数据库仍保留 `FileInfo`。

## 123 云盘 CLI

```yaml
LimeMeta:
  FileStore:
    Provider: "Pan123Cli"
    Pan123Cli:
      Command: "/usr/local/bin/pan123"
      ParentFileId: 123456
      UseDirectLink: true
      TempPath: "/tmp/limemeta-upload"
      Overwrite: false
```

部署前应单独验证 `pan123` 命令、登录状态、输出 JSON 格式和运行账户权限。启用直链时，下载接口会重定向到 Provider 返回的 URL。

## 自定义文件 Provider

实现：

```csharp
IFileStorageProvider
```

示例骨架：

```csharp
namespace LimeMetaService.Services;

using LimeMeta.Files;
using ModelFileInfo = LimeMeta.Models.FileInfo;

public sealed class ObjectStorageProvider : IFileStorageProvider
{
    public string Name => "ObjectStorage";

    public Task<FileStorageSaveResult> SaveAsync(
        Stream stream,
        string fileName,
        string? contentType,
        long size,
        CancellationToken ct)
    {
        // 上传到对象存储，并返回可持久化的 Provider 元数据。
        throw new NotImplementedException();
    }

    public Task<FileStorageOpenResult> OpenAsync(
        ModelFileInfo info,
        CancellationToken ct)
    {
        // 可以返回本地 FilePath，也可以返回短期签名 RedirectUrl。
        throw new NotImplementedException();
    }

    public Task DeleteAsync(
        ModelFileInfo info,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
```

注册：

```csharp
services.AddScoped<IFileStorageProvider, ObjectStorageProvider>();
```

并配置：

```yaml
LimeMeta:
  FileStore:
    Provider: "ObjectStorage"
```

Provider 的 `Name` 必须与配置一致且不能与其他 Provider 重名。

## WebSocket 配置

```yaml
LimeMeta:
  WebSocket:
    Path: "/api/ws"
    MaxMessageSize: 1048576
```

普通 HTTP 不接受 URL 中的 `access_token`，只有这里配置的 WebSocket 路径会读取：

```text
wss://api.example.com/api/ws?access_token=<JWT>
```

生产必须使用 `wss://`。反向代理需要正确转发 `Upgrade` 和 `Connection` 请求头，并把连接超时设置为适合长连接的值。

## 生产环境变量示例

Linux：

```bash
export ASPNETCORE_ENVIRONMENT="Production"
export LimeMeta__DataType="MySql"
export LimeMeta__ConnectionString="Server=mysql;Port=3306;Database=app;Uid=app;Pwd=<secret>;Charset=utf8mb4;"
export LimeMeta__AdminUserName="admin"
export LimeMeta__AdminUserPassword="<initial-admin-secret>"
export LimeMeta__JwtSignKey="<at-least-32-random-utf8-bytes>"
export LimeMeta__FileStore__Local__Path="/var/lib/limemeta/files"
```

不要通过进程命令行传递秘密，因为命令行参数可能被进程列表和运维采集记录。

## 发布

Windows：

```powershell
.\build-release.bat
```

或跨平台：

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet publish LimeMetaService.WebAPI/LimeMetaService.WebAPI.csproj \
  --configuration Release \
  --output .publish/LimeMetaService.WebAPI \
  /p:UseAppHost=false \
  --no-restore
```

发布目录应包含：

- WebAPI 和业务程序集。
- `appsettings.yml`。
- 对应环境的非秘密配置文件。
- `Seed/BeforeUpdateSchema.sql`、`Seed/AfterUpdateSchema.sql` 和模型 YAML。

秘密仍从部署环境注入。

## 上线检查表

- 使用独立数据库账号和最小必要权限。
- 生产连接串、管理员密码、JWT 密钥均来自秘密管理系统。
- HTTPS/WSS 已启用，反向代理转发配置正确。
- 已替换默认 `ILimeMetaAuthorizationService` 或确认默认策略满足需求。
- 自定义 GraphQL、HTTP、WebSocket 接口均显式授权。
- 已评估 `AutoSyncSchema` 的生产风险并完成备份。
- 管理员首次创建后已轮换初始密码。
- 上传大小、文件类型、恶意文件扫描和存储配额已限制。
- Local 文件目录已挂载持久存储并有正确权限。
- 日志中不记录密码、JWT、AppKey、连接串和业务敏感数据。
- MySQL 和 PostgreSQL 中实际使用的数据库至少完成一次启动、登录和 CRUD 冒烟测试。

## 常见故障

### 应用一启动就报 Options validation

检查当前环境名，以及连接串、JWT 密钥长度和管理员密码。Production 不允许模板示例值。

### 模型没有生成表或 GraphQL

检查：

1. 模型是否继承 `BaseObject`。
2. 是否有 `[Table]`。
3. DTO 是否与模型同命名空间并叫 `<模型名>Dto`。
4. `Extensions.cs` 是否仍调用 `AddLimeMetaModule`。
5. 是否已重新启动，而不只是热重载部分代码。

### 启动时报“缺少 Dto 定义”

DTO 的完整类型名不符合约定。模型 `A.B.Article` 对应的 DTO 必须是 `A.B.ArticleDto`。

### GraphQL 返回未认证

确认使用：

```text
Authorization: Bearer <token>
```

不要把 Token 放在普通 HTTP URL。检查 Token 是否过期，以及反向代理是否保留 `Authorization` 请求头。

### Seed 没更新

检查文件名是否与 CLR 模型类型完全一致、文件是否复制到发布目录、固定 ID 是否一致，以及文件最后修改时间是否晚于数据库行的 `Ver`。

### 文件上传成功但重启后找不到

Local 存储路径可能位于临时文件系统或容器层。改用持久卷，并保证所有实例访问同一存储，或切换到共享对象存储 Provider。

返回模板入口：[README.md](../README.md)。
