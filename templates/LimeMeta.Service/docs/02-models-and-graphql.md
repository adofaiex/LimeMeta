# 模型、DTO 与自动 GraphQL

## 一个完整模型示例

下面用“分类和文章”演示字段、导航属性、DTO 和树结构。

`Models/Category.cs`：

```csharp
namespace LimeMetaService.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using LimeMeta.Models;

[Table(Name = "category")]
[LimeMetaAuthorize("内容.分类")]
public sealed class Category : BaseParentChildren<Category>
{
    [Column(Name = "name", StringLength = 100)]
    public required string Name { get; set; }

    [Column(Name = "sn")]
    public int Sn { get; set; }

    [Navigate(nameof(Article.CategoryId))]
    public List<Article> Articles { get; set; } = [];
}

public sealed class CategoryDto : BaseParentChildrenDto
{
    public required string Name { get; set; }
    public int Sn { get; set; }
}
```

`Models/Article.cs`：

```csharp
namespace LimeMetaService.Models;

using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using LimeMeta.Models;

[Table(Name = "article")]
[LimeMetaAuthorize("内容.文章")]
public sealed class Article : BaseAudit
{
    [Column(Name = "title", StringLength = 200)]
    public required string Title { get; set; }

    [Column(Name = "content", StringLength = -1)]
    public string? Content { get; set; }

    [Column(Name = "published")]
    public bool Published { get; set; }

    [Column(Name = "category_id")]
    public Guid? CategoryId { get; set; }

    [Navigate(nameof(CategoryId))]
    public Category? Category { get; set; }
}

public sealed class ArticleDto : BaseDto
{
    public required string Title { get; set; }
    public string? Content { get; set; }
    public bool Published { get; set; }
    public Guid? CategoryId { get; set; }
}
```

DTO 是写入边界。导航属性通常不放进 DTO，只接收外键 `CategoryId`，可以避免客户端一次提交整棵对象图。

## 每个模型必须声明谁能操作

只要模型会生成自动 GraphQL API，就必须用 `[LimeMetaAuthorize]` 声明每种操作所需的权限：

```csharp
[LimeMetaAuthorize("工具", Create = "工具.上传")]
public sealed class Tool : BaseAudit
{
}
```

这段声明的实际含义是：

| 自动操作 | 需要的权限 |
| --- | --- |
| 查询、聚合 | `工具` |
| 新增 | `工具.上传` |
| 编辑 | `工具.编辑` |
| 删除 | `工具.删除` |

构造参数是读取权限和其他权限的默认前缀。`Read`、`Create`、`Update`、`Delete` 都可以按业务需要单独覆盖。同一权限组可以在多个模型上复用，例如 `Tool`、`ToolVersion` 和 `ToolVersionFile` 都可以使用 `工具` 这一组。

一个操作允许多个角色群体时，可以声明多个备选权限：

```csharp
[LimeMetaAuthorize(
    "工具",
    Read = "工具|审核管理.工具审核",
    Update = "工具.编辑|审核管理.工具审核")]
public sealed class Tool : BaseAudit
{
}
```

备选权限使用 `|` 分隔，用户满足其中任意一个即可。框架会自动清理分隔符两侧的空格，但会在启动时拒绝空项，例如 `工具||审核管理.工具审核`。权限名称本身不能包含 `|`。

这只解决“是否有资格开始这类操作”。例如审核员进入更新操作后只能修改审核状态，发布者只能维护自己的数据，仍应由 `ToolLogic` 检查。

权限本身仍在 `Seed/Perm.yaml` 中定义，并通过角色分配给用户。模型上的标记只是把自动 API 的操作对应到权限名称；它不会创建 Seed 数据。管理员始终可以执行所有自动操作。

如果确实只需要“登录即可”，必须明确写出允许哪些操作：

```csharp
[LimeMetaAllowAuthenticated(Read = true)]
public sealed class PublicNotice : BaseAudit
{
}
```

这里任意已登录用户可查询和聚合，新增、编辑、删除仍只允许管理员。业务模型没有声明访问策略，或者同时声明多种策略，应用都会在启动时失败，而不是默认放行。

## FreeSql 标记怎么用

常用标记：

```csharp
[Table(Name = "article")]
```

明确数据库表名。没有 `[Table]` 的类型不会被 LimeMeta 当作模型。

如果模型仍需参与数据库结构同步、Seed、Logic 和 `ILimeMeta` 数据操作，但不应生成自动 GraphQL 根字段，可以添加：

```csharp
using LimeMeta.Attributes;

[Table(Name = "internal_job")]
[DisableGraphQL]
public sealed class InternalJob : BaseAudit
{
}
```

`[DisableGraphQL]` 会关闭该模型的自动查询、聚合以及增删改 Mutation。模型仍属于 LimeMeta 模型，因此仍需提供 `<ModelName>Dto`。它不会自动隐藏其他已公开模型上的导航属性；如果导航属性也不应出现在 Schema 中，请在该属性上添加 HotChocolate 的 `[GraphQLIgnore]`。

```csharp
[Column(Name = "title", StringLength = 200)]
```

明确列名和字符串长度。大文本可以用 `StringLength = -1`。

```csharp
[Navigate(nameof(CategoryId))]
public Category? Category { get; set; }
```

声明多对一/一对一导航，参数是当前模型中的外键属性。

```csharp
[Navigate(nameof(Article.CategoryId))]
public List<Article> Articles { get; set; } = [];
```

声明一对多导航，参数是对方模型中的外键属性。

多对多关系需要显式关系模型：

```csharp
[Navigate(ManyToMany = typeof(ArticleTag))]
public List<Tag> Tags { get; set; } = [];
```

关系模型同样继承 `BaseObject`、标记 `[Table]`，并提供同命名 DTO。

需要索引的字段可以添加 LimeMeta 的 `[Indexed]`：

```csharp
using LimeMeta.Attributes;

[Column(Name = "code", StringLength = 50), Indexed]
public required string Code { get; set; }
```

唯一性等强约束应同时在数据库索引和业务校验中体现，不要只依赖 GraphQL 输入校验。

## DTO 的规则和安全意义

`insert<Model>` 使用 DTO，而不是直接使用数据库模型。这样可以：

- 排除只应由服务端维护的字段。
- 避免客户端提交导航对象。
- 避免密码哈希、内部状态等敏感字段成为输入。
- 给未来的输入兼容留出空间。

DTO 必须：

- 继承 `BaseDto`，树模型 DTO 通常继承 `BaseParentChildrenDto`。
- 与模型在同一个命名空间。
- 准确命名为 `<ModelName>Dto`。

DTO 中的属性会由 AutoMapper 按名称映射到模型。类型或名称不匹配时，应显式调整模型/DTO，而不是假设框架会进行复杂转换。

## 自动生成的查询

对于 `Article`，查询字段是 `Article`：

```graphql
query {
  Article(page: { index: 1, size: 10 }) {
    index
    size
    total
    items {
      id
      title
      published
    }
  }
}
```

分页参数：

| 字段 | 默认值 | 含义 |
| --- | --- | --- |
| `index` | `1` | 从 1 开始的页码 |
| `size` | `10` | 每页条数；小于等于 0 时不分页 |

生产接口不建议允许客户端长期使用 `size <= 0`，可在网关、自定义授权或专用查询中限制结果规模。

### 过滤

过滤语法由 HotChocolate 提供：

```graphql
query {
  Article(
    where: {
      and: [
        { published: { eq: true } }
        { title: { contains: "LimeMeta" } }
      ]
    }
  ) {
    total
    items {
      id
      title
    }
  }
}
```

可用操作由字段类型决定，常见操作包括 `eq`、`neq`、`in`、`contains`、`startsWith`、`gt`、`gte`、`lt` 和 `lte`。以当前 GraphQL IDE 的 Schema 提示为准。

### 排序

```graphql
query {
  Article(order: [{ published: DESC }, { created: DESC }]) {
    items {
      id
      title
      created
    }
  }
}
```

### 导航属性

只要在选择集中请求导航属性，框架会根据选择树构建 FreeSql Include：

```graphql
query {
  Article {
    items {
      id
      title
      category {
        id
        name
        parent {
          id
          name
        }
      }
    }
  }
}
```

不要无边界地请求深层双向导航，否则响应和 SQL 成本会快速增加。对前端固定页面，专用查询往往比任意深度通用查询更容易控制。

## 自动生成的写入

### 新增

```graphql
mutation {
  insertArticle(
    objs: [{
      title: "第一篇文章"
      content: "正文"
      published: false
      categoryId: "分类 ID"
    }]
  )
}
```

返回新增对象的 ID 列表。框架会：

1. 把 DTO 映射成模型。
2. 调用授权服务检查 `Insert`。
3. 触发 `BeforeInsert`。
4. 插入数据库。
5. 触发 `AfterInsert`。

### 部分更新

```graphql
mutation {
  updateArticle(
    objs: [{
      id: "文章 ID"
      published: true
    }]
  )
}
```

更新输入是动态 JSON。必须提供 `id`，只更新实际出现的属性。字段名按模型属性匹配，推荐直接使用 GraphQL Schema 中显示的名称。

需要“状态只能从草稿变为已发布”之类的规则时，在 Logic 或专用 Mutation 中校验旧对象和新对象，不要只依赖客户端。

### 删除

```graphql
mutation {
  deleteArticle(ids: ["文章 ID 1", "文章 ID 2"])
}
```

返回受影响行数。默认是物理删除；框架没有自动软删除语义。需要软删除时，可以加入状态字段并禁止通用 Delete，或在自定义授权中拒绝 Delete，改用专用接口。

## 聚合

聚合字段名是 `<ModelName>Aggr`：

```graphql
query {
  ArticleAggr(
    fields: [
      { type: COUNT, name: "Id" }
    ]
    groups: ["Published"]
  )
}
```

聚合类型：

- `COUNT`
- `AVG`
- `MIN`
- `MAX`
- `SUM`

`name` 和 `groups` 按模型 CLR 属性名解析，不是数据库列名。返回值是动态 JSON；聚合结果的键由属性名和聚合类型组成，例如 `IdCount`，分组字段按传入名称返回。

带过滤的聚合：

```graphql
query {
  ArticleAggr(
    where: { created: { gte: 20260701000000000 } }
    fields: [{ type: COUNT, name: "Id" }]
    groups: ["Published"]
  )
}
```

复杂报表、跨表指标或需要严格稳定返回结构的查询，建议写专用 GraphQL Query，而不是依赖动态聚合 JSON。

## 树模型

分类、菜单等模型可继承：

```csharp
public sealed class Category : BaseParentChildren<Category>
```

新增时 DTO 只需提供 `parentId`：

```graphql
mutation {
  insertCategory(
    objs: [{
      name: "后端开发"
      parentId: "父分类 ID"
      sn: 10
    }]
  )
}
```

插入后，`ParentChildrenLogic` 会计算 `Path` 和 `NamePath`。模型如果没有名为 `Name` 的属性，则只自动计算 ID 路径。

修改父级后会重算当前节点的路径。设计树数据时应避免环形父子关系；框架不会把任意图结构自动修复成树。

## 在业务服务中直接使用 `ILimeMeta`

```csharp
namespace LimeMetaService.Services;

using LimeMeta.Data;
using LimeMetaService.Models;

public sealed class ArticleService(ILimeMeta meta)
{
    public Article? Find(Guid id)
        => meta.Query<Article>()
            .Include(x => x.Category)
            .FirstOrDefault(x => x.Id == id);

    public int Publish(Guid id, Guid userId)
    {
        var article = meta.Query<Article>().FirstOrDefault(x => x.Id == id)
            ?? throw new InvalidOperationException("文章不存在。");

        article.Published = true;
        return meta.Update(
            [article],
            [nameof(Article.Published)],
            userId);
    }
}
```

在 `Extensions.cs` 注册：

```csharp
services.AddScoped<ArticleService>();
```

注意：

- `Insert`、`Update`、`Delete` 默认触发 Logic。
- 传入 `userId` 才能正确记录审计用户。
- `Query<T>()` 本身不触发查询 Logic。
- 直接调用 `ILimeMeta` 不会自动调用 GraphQL 的 `ILimeMetaAuthorizationService`。自定义服务或 Endpoint 必须主动做授权。

## 哪些情况不应使用自动 CRUD

遇到以下情况，优先写专用接口：

- 操作包含多个模型并要求事务一致性。
- 状态迁移有严格顺序。
- 需要调用支付、消息、对象存储等外部服务。
- 不同用户只能看到自己的数据。
- 输入与数据库模型差异很大。
- 需要稳定、版本化的对外 API 契约。

下一篇：[用户、角色、权限与安全](03-users-and-authorization.md)。
