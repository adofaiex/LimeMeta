# LimeMetaService 开发指南

这是由 `LimeMeta.Templates` 生成的 .NET 10 后端项目。生成结果已经包含 `LimeMeta` 与 `LimeMeta.GraphQL` 的完整框架源码：只要定义一个模型和对应 DTO，框架就会完成数据库表同步，并生成带分页、过滤、排序、聚合、增删改和授权检查的 GraphQL 接口。

模板默认使用 MySQL。业务代码放在 `LimeMetaService/`，宿主、环境配置、种子数据和发布入口放在 `LimeMetaService.WebAPI/`；框架实现位于同一解决方案的 `LimeMeta/` 和 `LimeMeta.GraphQL/`，可以直接调试和修改。

## 先在 10 分钟内跑起来

### 1. 还原第三方依赖

框架源码已经包含在当前解决方案中，不需要下载 LimeMeta 框架包。FreeSql、HotChocolate 等第三方依赖仍从 NuGet.org 还原。

```powershell
dotnet restore
```

如需固定源，可在本机 `NuGet.config` 中保留 `https://api.nuget.org/v3/index.json`。

### 2. 准备 MySQL

```sql
CREATE DATABASE limemeta_service
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
```

打开 `LimeMetaService.WebAPI/appsettings.Development.yml`，把连接串中的用户名和密码改成自己的本地 MySQL 配置：

```yaml
LimeMeta:
  DataType: "MySql"
  ConnectionString: "Server=127.0.0.1;Port=3306;Database=limemeta_service;Uid=root;Pwd=change-me;Charset=utf8mb4;"
```

Development 文件里的管理员密码和 JWT 密钥只是本地示例。不要把它们用于测试、预发布或生产环境。

### 3. 启动

```powershell
dotnet restore
dotnet run --project LimeMetaService.WebAPI
```

首次启动会自动：

1. 执行 `Seed/BeforeUpdateSchema.sql`。
2. 根据内置模型和业务模型同步表结构。
3. 执行 `Seed/AfterUpdateSchema.sql`。
4. 创建管理员权限、管理员角色和管理员用户。
5. 加载名称与模型类型一致的 YAML 种子文件。

打开 GraphQL：

```text
http://127.0.0.1:6675/api/gql
```

### 4. 登录

```graphql
mutation {
  login(username: "admin", password: "change-me-admin-password") {
    name
    token
  }
}
```

复制返回的 `token`，后续 HTTP 请求添加请求头：

```text
Authorization: Bearer <token>
```

GraphQL 浏览器通常可以在请求 Headers 区域填写：

```json
{
  "Authorization": "Bearer <token>"
}
```

如果登录结果中的 `token` 是 `null`，表示用户名或密码错误。框架不会在错误信息中区分两者。

## 常见安装与环境问题

- 安装了模板但 `dotnet new limemeta` 仍找不到模板？
  - 先确认模板安装成功：`dotnet new list` 里能看到 `LimeMeta Service`。
  - 如有缓存干扰，执行：
    ```powershell
    dotnet new uninstall LimeMeta.Templates
    dotnet new install LimeMeta.Templates
    ```

- 明明已发布但搜不到模板？
  - NuGet 上线有索引延迟，可以稍后重试或按已知模板版本安装：
    ```powershell
    dotnet new install LimeMeta.Templates
    ```

## 写第一个业务模型

在 `LimeMetaService/Models/Article.cs` 新建：

```csharp
namespace LimeMetaService.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using LimeMeta.Models;

[Table(Name = "article")]
[LimeMetaAuthorize("文章")]
public sealed class Article : BaseAudit
{
    [Column(Name = "title", StringLength = 200)]
    public required string Title { get; set; }

    [Column(Name = "content", StringLength = -1)]
    public string? Content { get; set; }

    [Column(Name = "published")]
    public bool Published { get; set; }
}

public sealed class ArticleDto : BaseDto
{
    public required string Title { get; set; }
    public string? Content { get; set; }
    public bool Published { get; set; }
}
```

重新启动后，框架会发现 `Article` 并创建 `article` 表，同时生成：

- `Article`：分页、过滤、排序和导航属性查询。
- `ArticleAggr`：分组与聚合查询。
- `insertArticle`：使用 `ArticleDto` 作为输入。
- `updateArticle`：只更新请求中实际提供的字段。
- `deleteArticle`：按 ID 批量删除。

四个不可省略的约定：

1. 模型必须继承 `BaseObject`、`BaseAudit` 或 `BaseParentChildren<T>`。
2. 模型必须标记 FreeSql 的 `[Table]`。
3. DTO 必须和模型处于同一命名空间，并准确命名为 `<模型名>Dto`。
4. 模型必须选择 `[LimeMetaAuthorize]`、`[LimeMetaAllowAuthenticated]` 或 `[DisableGraphQL]` 中的一种访问策略。

上面的 `[LimeMetaAuthorize("文章")]` 表示：

- 查询和聚合需要 `文章` 权限。
- 新增需要 `文章.新增` 权限。
- 编辑需要 `文章.编辑` 权限。
- 删除需要 `文章.删除` 权限。

默认权限名可以按业务词汇单独覆盖，例如 `[LimeMetaAuthorize("工具", Create = "工具.上传")]`。把这些权限名称写入 `LimeMetaService.WebAPI/Seed/Perm.yaml` 后，再通过角色分配给用户。管理员始终可以执行全部操作。

如果某些操作只要求用户登录，可以使用 `[LimeMetaAllowAuthenticated(Read = true)]`；示例表示任意登录用户可查询和聚合，但新增、编辑、删除仍只有管理员可执行。

如果模型需要数据库同步、Seed、Logic 和 `ILimeMeta`，但不应生成自动 GraphQL 查询、聚合和 Mutation，请使用 `[DisableGraphQL]`。该模型仍需提供 `<ModelName>Dto`。如果已公开模型的导航属性指向该模型，还应在导航属性上添加 HotChocolate 的 `[GraphQLIgnore]`。

框架会在应用启动时检查这项声明。业务模型未声明访问策略，或者同时声明多种策略，都会直接启动失败，避免新接口意外对所有登录用户开放。

新增一条数据：

```graphql
mutation {
  insertArticle(
    objs: [{
      title: "LimeMeta 入门"
      content: "第一篇文章"
      published: true
    }]
  )
}
```

查询：

```graphql
query {
  Article(
    page: { index: 1, size: 20 }
    where: { published: { eq: true }, title: { contains: "LimeMeta" } }
    order: [{ created: DESC }]
  ) {
    index
    size
    total
    items {
      id
      title
      content
      published
      created
      creatorId
    }
  }
}
```

部分更新：

```graphql
mutation {
  updateArticle(
    objs: [{
      id: "替换成文章 ID"
      title: "修改后的标题"
    }]
  )
}
```

这里没有提供 `content` 和 `published`，它们不会被清空。删除：

```graphql
mutation {
  deleteArticle(ids: ["替换成文章 ID"])
}
```

## 项目结构

```text
LimeMetaService/
├── LimeMetaService.sln
├── README.md                         当前入口文档
├── docs/                             按主题拆分的完整开发文档
├── LICENSE
├── NOTICE
├── build-release.bat                 Windows 发布脚本
├── LimeMeta/                         框架核心源码，可直接修改
├── LimeMeta.GraphQL/                 自动 GraphQL 框架源码，可直接修改
├── LimeMetaService/                  业务类库
│   ├── Extensions.cs                 业务模块、DI、GraphQL 扩展注册
│   ├── Models/                       模型和 DTO
│   ├── Logics/                       模型生命周期规则
│   ├── TypeExtensions/               GraphQL Query/Mutation 扩展
│   ├── Services/                     领域服务和第三方服务封装
│   ├── Configuration/                业务配置类型
│   └── Common/                       通用类型
└── LimeMetaService.WebAPI/           ASP.NET Core 宿主
    ├── Program.cs                    注册和中间件入口
    ├── appsettings.yml               无秘密的公共配置
    ├── appsettings.Development.yml   本机开发示例
    ├── WebSockets/                   WebSocket 消息控制器
    └── Seed/
        ├── BeforeUpdateSchema.sql
        └── AfterUpdateSchema.sql
```

`Program.cs` 已经调用：

```csharp
builder.Services.AddLimeMeta(builder.Configuration, builder.Environment);
var gqlBuilder = builder.Services.AddLimeMetaGraphQL();
builder.Services.AddLimeMetaService(builder.Configuration, gqlBuilder);

app.UseLimeMeta();
app.UseLimeMetaService();
app.UseLimeMetaGraphQL();
```

而业务项目的 `Extensions.cs` 已调用 `AddLimeMetaModule` 和 `UseLimeMetaModule`。这两处负责让框架明确扫描业务程序集中的模型、DTO 和 Logic，通常不要删除。

## 框架已经内置什么

| 能力 | 默认行为 |
| --- | --- |
| 数据访问 | 基于 FreeSql，模板默认 MySQL，也保留 PostgreSQL Provider |
| 表结构 | 启动时按模型同步，可用配置关闭 |
| GraphQL | 自动生成分页、过滤、排序、导航查询、聚合和 CRUD |
| 用户 | BCrypt 服务端哈希，登录和专用用户管理 Mutation |
| 角色与权限 | 用户、部门、角色、权限以及四张关联表 |
| 授权 | 所有自动 GraphQL 操作先经过 `ILimeMetaAuthorizationService` |
| Logic | Select/Insert/Update/Delete 的 Before/After 生命周期 |
| 审计 | `BaseAudit` 自动维护创建人、修改人和时间 |
| 树结构 | `BaseParentChildren<T>` 自动维护 ID 路径和名称路径 |
| HTTP | FastEndpoints，开发环境提供 Swagger |
| 文件 | 本地存储和 123 云盘 CLI Provider，统一上传/下载接口 |
| WebSocket | 单入口消息分发、用户连接管理、单播和广播 |
| 种子 | `<ModelType>.yaml` 增量加载和建表前后 SQL |

最重要的边界是：自动接口只负责通用模型操作。带有业务语义的动作，例如“发布文章”“审批订单”“退款”，应该写成专用 GraphQL Mutation、FastEndpoints Endpoint 或领域服务，不应该伪装成普通字段更新。

## 内置用户与权限

框架内置这些核心模型：

- `User`：用户；密码哈希永远不进入 GraphQL Schema、DTO 或响应。
- `Role`：树形角色。
- `Perm`：树形权限定义。
- `Dept`：树形部门/组织。
- `UserRole`、`RolePerm`、`DeptUser`、`DeptRole`：关系模型。
- `AppKey`：通过请求头代表某个用户调用 HTTP 接口。
- `Message`、`MessageUser`：消息与已读关系。
- `FileInfo`：文件元数据。

登录和用户密码不使用通用 CRUD。框架提供：

- `login`
- `createUser`
- `updateUser`
- `deleteUser`
- `changePassword`
- `resetUserPassword`

管理员可管理其他用户；普通用户只能修改自己的密码。内置系统模型的新增、修改和删除仅允许管理员。业务模型必须用 `[LimeMetaAuthorize]`、`[LimeMetaAllowAuthenticated]` 或 `[DisableGraphQL]` 明确访问策略，未声明就不能启动，因此通常不再需要维护按模型分支的自定义授权服务。

详细关系、继承规则、用户 Mutation 示例和自定义授权见 [用户、角色、权限与安全](docs/03-users-and-authorization.md)。

## 下一步该读哪一篇

- [框架结构与内置能力](docs/01-overview.md)：先建立完整心智模型，了解每个目录和内置模型。
- [模型、DTO 与自动 GraphQL](docs/02-models-and-graphql.md)：字段、关联、树模型、CRUD、过滤、排序和聚合。
- [用户、角色、权限与安全](docs/03-users-and-authorization.md)：管理员初始化、角色/部门关系、模型权限、密码、JWT 和 AppKey。
- [Logic、HTTP 接口、GraphQL 扩展与 WebSocket](docs/04-logic-and-extensions.md)：写业务规则和新的接口。
- [配置、种子、文件存储与部署](docs/05-configuration-and-deployment.md)：MySQL/PostgreSQL、环境变量、Seed、发布和故障排查。

推荐阅读顺序是 `01 → 02 → 03 → 04 → 05`。如果只是要增加一个简单实体，直接从第 02 篇开始即可。

## 生产环境最少配置

`appsettings.yml` 故意不保存真实秘密。生产环境至少注入：

```bash
export ASPNETCORE_ENVIRONMENT="Production"
export LimeMeta__ConnectionString="Server=127.0.0.1;Port=3306;Database=app;Uid=app;Pwd=strong-password;Charset=utf8mb4;"
export LimeMeta__AdminUserPassword="a-long-random-admin-password"
export LimeMeta__JwtSignKey="a-random-secret-with-at-least-32-utf8-bytes"
```

Windows PowerShell：

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:LimeMeta__ConnectionString = "Server=127.0.0.1;Port=3306;Database=app;Uid=app;Pwd=strong-password;Charset=utf8mb4;"
$env:LimeMeta__AdminUserPassword = "a-long-random-admin-password"
$env:LimeMeta__JwtSignKey = "a-random-secret-with-at-least-32-utf8-bytes"
```

生产环境缺少连接串、管理员初始密码或至少 32 个 UTF-8 字节的 JWT 密钥时，应用会拒绝启动。生产部署必须启用 HTTPS。

## 构建与发布

Windows：

```powershell
.\build-release.bat
```

输出：

```text
.publish/LimeMetaService.WebAPI/
.publish/LimeMetaService.WebAPI.zip
```

跨平台也可以直接执行：

```bash
dotnet publish LimeMetaService.WebAPI/LimeMetaService.WebAPI.csproj \
  --configuration Release \
  --output .publish/LimeMetaService.WebAPI \
  /p:UseAppHost=false
```

## 修改与更新框架源码

`LimeMeta/` 和 `LimeMeta.GraphQL/` 是生成时取得的独立源码快照，通过下面的项目引用参与编译：

```xml
<ProjectReference Include="..\LimeMeta\LimeMeta.csproj" />
<ProjectReference Include="..\LimeMeta.GraphQL\LimeMeta.GraphQL.csproj" />
```

可以像修改业务代码一样直接修改这两个目录，重新构建后立即生效。安装新版模板不会覆盖当前项目，也没有自动同步上游源码的脚本。

需要引入新版本框架实现时，先创建一个临时的新模板项目，与当前两个框架目录进行人工比较，再有选择地合并。已经做过业务定制的框架源码不要直接整目录覆盖。

