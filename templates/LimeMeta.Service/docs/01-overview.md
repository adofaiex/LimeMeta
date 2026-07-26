# 框架结构与内置能力

## 先建立一个心智模型

LimeMeta 的核心流程可以概括为：

```text
模型 + DTO
    ↓ 业务程序集扫描
FreeSql 表结构与数据访问
    ↓
Logic 生命周期
    ↓
自动 GraphQL 查询 / 聚合 / CRUD
    ↓
统一认证与 ILimeMetaAuthorizationService 授权
```

开发一个普通业务实体时，你通常只写模型和 DTO；需要校验、补值或数据范围时再写 Logic；需要有明确业务语义的动作时，再写专用 GraphQL、HTTP 或 WebSocket 接口。

框架不会代替领域设计。比如订单模型可以自动得到通用 CRUD，但“支付”“取消”“确认收货”仍应该是单独的业务接口。

## 四个项目如何分工

### `LimeMeta`

框架核心源码，包括数据访问、Logic 生命周期、认证授权、文件存储、FastEndpoints 和 WebSocket。它是当前项目的一部分，可以直接调试和修改。

### `LimeMeta.GraphQL`

自动 GraphQL 查询、聚合和 Mutation 的源码项目，通过 `ProjectReference` 引用 `LimeMeta`。

### `LimeMetaService`

这是业务类库，应该放：

- 数据库模型和 DTO。
- 模型生命周期 Logic。
- 领域服务和第三方服务封装。
- FastEndpoints Endpoint。
- GraphQL Query/Mutation Type Extension。
- WebSocket 消息控制器。
- 业务配置类型和通用代码。

### `LimeMetaService.WebAPI`

这是 ASP.NET Core 宿主，应该放：

- `Program.cs`。
- 不同环境的 YAML 配置。
- `Seed` 种子数据和建表前后 SQL。
- 只属于部署宿主的代码。

把主要业务代码留在业务类库，可以避免 Web 宿主逐渐变成无法复用和测试的“大项目”。

两个框架目录是模板生成时的独立源码快照。安装新版模板不会自动更新它们；本项目对框架源码的修改应和业务代码一起评审、测试和提交。

## 启动时到底发生了什么

`Program.cs` 的注册顺序是：

```csharp
builder.Services.AddLimeMeta(builder.Configuration, builder.Environment);
var gqlBuilder = builder.Services.AddLimeMetaGraphQL();
builder.Services.AddLimeMetaService(builder.Configuration, gqlBuilder);
```

其中：

- `AddLimeMeta` 注册配置验证、FreeSql、`ILimeMeta`、Logic、JWT、FastEndpoints、文件存储和 WebSocket。
- `AddLimeMetaGraphQL` 注册 HotChocolate，并为框架发现的模型准备自动类型。
- `AddLimeMetaService` 是当前业务模块的注册入口，可以继续注册领域服务、自定义授权和 GraphQL 扩展。

中间件顺序是：

```csharp
app.UseLimeMeta();
app.UseLimeMetaService();
app.UseLimeMetaGraphQL();
```

其中：

- `UseLimeMeta` 执行表结构同步、种子加载，并启用认证、授权、WebSocket、FastEndpoints 和开发环境 Swagger。
- `UseLimeMetaService` 再次确认当前业务程序集中的模型和 Logic 已注册。
- `UseLimeMetaGraphQL` 映射 `/api/gql`。

模板的 `Extensions.cs` 中这两句很重要：

```csharp
services.AddLimeMetaModule(typeof(Extensions).Assembly);
app.UseLimeMetaModule(typeof(Extensions).Assembly);
```

它们让框架明确扫描业务程序集。不要依赖“程序集碰巧已经加载”，也不要随意删除。

## 模型发现规则

一个类型只有同时满足下面条件，才会成为 LimeMeta 模型：

1. 是非抽象类。
2. 继承 `BaseObject`。
3. 带有 FreeSql `[Table]`。
4. 存在同程序集、同命名空间、名称为 `<模型名>Dto` 的 DTO。

例如：

```text
LimeMetaService.Models.Article
LimeMetaService.Models.ArticleDto
```

下面这些写法会导致启动失败或模型未被发现：

```text
ArticleInput              名称不符合约定
Dtos.ArticleDto           DTO 与模型不在同一命名空间
Article 未标记 [Table]    不会被扫描
Article 未继承 BaseObject 不会被扫描
```

## 三种模型基类

| 基类 | 自动字段/行为 | 适用场景 |
| --- | --- | --- |
| `BaseObject` | `Id`、`Ver` | 关系表、简单实体 |
| `BaseAudit` | `BaseObject` 的字段，加创建/修改时间与用户 | 大多数业务实体 |
| `BaseParentChildren<T>` | `BaseAudit` 的字段，加父子关系、`Path`、`NamePath` | 分类、组织、菜单等树结构 |

`BaseObject.Id` 默认是 `Guid` 主键。`Ver` 是框架版本字段，也用于判断 YAML 种子是否需要覆盖已有数据，不要把它当成业务版本号。

`BaseAudit` 自动维护：

- `Created`、`CreatorId`、`Creator`
- `Updated`、`ModifierId`、`Modifier`

时间使用 `yyyyMMddHHmmssfff` 的长整数形式，例如 `20260724153010123`。创建/修改用户来自本次 `ILimeMeta` 操作传入的 `userId`。

`BaseParentChildren<T>` 自动维护：

- `ParentId`、`Parent`、`Children`
- `Path`：由祖先和当前对象 ID 组成。
- `NamePath`：当模型有 `Name` 属性时，由祖先和当前名称组成。

删除一个树节点时，框架会递归删除直接子节点。涉及重要业务数据时，应自行增加“禁止删除非空节点”或软删除规则。

## 内置模型全览

### 身份与权限

| 模型 | 含义 | 关键字段/关系 |
| --- | --- | --- |
| `User` | 用户 | `Name`、`Username`、`Phone`、角色、消息 |
| `Role` | 树形角色 | `Name`、`Sn`、用户、权限 |
| `Perm` | 树形权限定义 | `Name`、`Sn` |
| `Dept` | 树形部门/组织 | 名称、别名、联系信息、组织类型 |
| `UserRole` | 用户直接拥有角色 | `UserId`、`RoleId` |
| `RolePerm` | 角色拥有权限 | `RoleId`、`PermId` |
| `DeptUser` | 用户所在部门 | `DeptId`、`UserId` |
| `DeptRole` | 部门拥有角色 | `DeptId`、`RoleId` |
| `AppKey` | 代表用户的应用密钥 | `Key`、`Expired`、`UserId` |

密码只以完整 BCrypt 哈希保存在 `User.PasswordHash` 中。该字段不会进入 GraphQL Schema、`UserDto` 或正常 JSON 响应。

### 消息与文件

| 模型 | 含义 |
| --- | --- |
| `Message` | 标题、正文、来源模型和来源 ID |
| `MessageUser` | 消息与用户的关系，并记录 `Read` |
| `FileInfo` | 文件名、大小、哈希、存储 Provider、内部路径和外部 URL |

这些内置模型同样能被 GraphQL 查询。对内置系统模型的修改默认要求管理员权限。

## 自动 GraphQL 的边界

每个业务模型会自动得到：

```text
<ModelName>
<ModelName>Aggr
insert<ModelName>
update<ModelName>
delete<ModelName>
```

但有两个特例：

- `User` 不生成通用增删改，改用安全的专用用户 Mutation。
- `Perm` 不生成通用增删改，权限定义建议通过 Seed 或经过明确管理员校验的专用接口维护。

自动 GraphQL 不等于“自动安全”。默认策略只区分系统模型和业务模型：

- 所有查询和聚合：已认证用户可用。
- 业务模型增删改：已认证用户可用。
- 内置系统模型增删改：管理员可用。

真实项目上线前通常要替换 `ILimeMetaAuthorizationService`，按权限名、租户、数据归属或模型类型限制操作。

## `ILimeMeta` 是什么

`ILimeMeta` 是业务代码访问框架数据能力的主要接口：

```csharp
public sealed class ArticleService(ILimeMeta meta)
{
    public Article? Find(Guid id)
        => meta.Query<Article>().FirstOrDefault(x => x.Id == id);

    public int Create(Article article, Guid userId)
        => meta.Insert([article], userId);
}
```

常用方法：

- `Query<T>()`：得到 FreeSql `ISelect<T>`。
- `Insert`、`Update`、`Delete`：执行写操作并触发 Logic。
- `Select`：执行分页查询，并触发查询前后 Logic。
- `Aggr`：执行聚合，并触发查询前 Logic。
- `UpdateSchema`、`LoadSeed`：手动触发表结构和种子流程。

需要特别注意：

```csharp
meta.Query<Article>().ToList();
```

只是获得并直接执行 FreeSql 查询，不会触发 `BeforeSelect` 或 `AfterSelect`。自动 GraphQL 会把查询交给 `meta.Select(...)`，所以会触发查询 Logic。自定义服务如果依赖查询 Logic，应使用 `Select`，或主动把数据范围条件写在查询中。

## 默认认证入口

| 场景 | 传递凭据 |
| --- | --- |
| 普通 HTTP / GraphQL | `Authorization: Bearer <token>` |
| AppKey | `x-limemeta-app-key: <guid>` |
| WebSocket 握手 | `/api/ws?access_token=<token>` |

普通 HTTP 请求不会从 URL 查询参数读取 JWT 或 AppKey。这样可以降低 Token 被代理日志、浏览器历史或监控系统记录的风险。

## 推荐开发步骤

开发一个新模块时，按这个顺序最容易控制复杂度：

1. 先写模型和 DTO，启动并检查表结构。
2. 在 GraphQL IDE 中确认自动查询和 CRUD。
3. 写 Logic，处理字段清洗、审计外的补值、校验和数据范围。
4. 为“动作型”业务写专用 GraphQL Mutation 或 FastEndpoints Endpoint。
5. 在自定义授权服务中为模型操作设置权限。
6. 增加固定 ID 的 Seed 或迁移 SQL。
7. 添加单元测试和数据库集成测试。

接下来阅读 [模型、DTO 与自动 GraphQL](02-models-and-graphql.md)。
