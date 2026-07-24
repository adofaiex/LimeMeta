# Logic、HTTP 接口、GraphQL 扩展与 WebSocket

## 什么时候写 Logic

Logic 适合所有数据入口都必须遵守的模型规则，例如：

- 清理或规范化字段。
- 校验状态和字段组合。
- 自动补充业务字段。
- 限制查询数据范围。
- 写入后产生站内消息。
- 删除前检查或清理关联数据。

如果一个动作只属于某个明确用例，或包含外部调用、事务编排和复杂返回值，优先写领域服务和专用接口。

## 一个完整的 Logic

`Logics/ArticleLogic.cs`：

```csharp
namespace LimeMetaService.Logics;

using LimeMeta.Logics;
using LimeMetaService.Models;

public sealed class ArticleLogic : BaseLogic<Article>
{
    public ArticleLogic(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory, scopeFactory)
    {
        BeforeSelect += OnBeforeSelect;
        BeforeInsert += OnBeforeInsert;
        BeforeUpdate += OnBeforeUpdate;
        BeforeDelete += OnBeforeDelete;
    }

    private static void OnBeforeSelect(
        object? sender,
        BeforeSelectEventArgs<Article> args)
    {
        // 示例：匿名上下文不返回未发布文章。
        // 自动 GraphQL 本身要求登录，这个判断主要服务其他调用入口。
        if (args.UserId is null)
        {
            args.Query = args.Query.Where(x => x.Published);
        }
    }

    private static void OnBeforeInsert(
        object? sender,
        BeforeInsertEventArgs<Article> args)
    {
        foreach (var article in args.Objs)
        {
            article.Title = article.Title.Trim();
            if (article.Title.Length == 0)
            {
                throw new InvalidOperationException("文章标题不能为空。");
            }
        }
    }

    private static void OnBeforeUpdate(
        object? sender,
        BeforeUpdateEventArgs<Article> args)
    {
        foreach (var (oldArticle, newArticle) in args.Objs)
        {
            newArticle.Title = newArticle.Title.Trim();

            if (oldArticle.Published && !newArticle.Published)
            {
                throw new InvalidOperationException("已发布文章不能退回草稿。");
            }
        }
    }

    private static void OnBeforeDelete(
        object? sender,
        BeforeDeleteEventArgs<Article> args)
    {
        if (args.Objs.Any(x => x.Published))
        {
            throw new InvalidOperationException("已发布文章不能直接删除。");
        }
    }
}
```

只要业务程序集仍由 `AddLimeMetaModule` 注册，Logic 会自动发现，不需要逐个 `AddScoped<ArticleLogic>()`。

## 全部生命周期事件

| 事件 | 关键数据 | 能做什么 |
| --- | --- | --- |
| `Created` | `ILogicManager` | Logic 创建后执行一次初始化 |
| `BeforeSelect` | 可替换的 `Query` | 追加数据范围、租户和软删除条件 |
| `AfterSelect` | 查询结果 `Objs` | 结果后处理 |
| `BeforeInsert` | 待插入 `Objs` | 校验、清洗、补值 |
| `AfterInsert` | 已插入 `Objs` | 创建关联、消息或审计外记录 |
| `BeforeUpdate` | `旧对象 → 新对象` 字典 | 比较状态、阻止非法修改 |
| `AfterUpdate` | `旧对象 → 新对象` 字典 | 更新关联或产生事件 |
| `BeforeDelete` | 将删除的 `Objs` | 阻止删除、清理关系 |
| `AfterDelete` | 已删除的 `Objs` | 外部清理或通知 |

每个事件参数还包含：

- `LimeMeta`：当前 `ILimeMeta`。
- `ModelType`：实际模型类型。
- `UserId`：发起操作的用户。
- `Context`：调用上下文；自动 GraphQL 中通常是 Resolver Context。

## Logic 的执行顺序

适用同一个模型的 Logic 会按 `Order` 从小到大执行。

- 框架基础对象 Logic 的顺序是 `0`。
- 针对接口的 Logic 默认接近基础阶段。
- 针对具体模型的 `BaseLogic<T>` 默认顺序是 `100`。

业务 Logic 可以在构造函数中调整：

```csharp
public ArticleLogic(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : base(loggerFactory, scopeFactory)
{
    Order = 200;
}
```

不要依赖同一 `Order` 下不明确的相对顺序；有依赖关系时显式使用不同值。

## 避免 Logic 递归

在 Logic 中再次调用 `ILimeMeta.Insert/Update/Delete`，默认会再次触发对应 Logic，可能造成递归。

必要时可以把 `enableLogic` 设为 `false`：

```csharp
args.LimeMeta.Update(
    [article],
    [nameof(Article.Published)],
    args.UserId,
    enableLogic: false);
```

这会跳过所有 Logic，包括审计和框架内置规则，必须谨慎使用。通常只应在已经由当前 Logic 完整维护好数据时使用。

`meta.Query<T>()` 不触发查询 Logic；`meta.Select(...)` 才触发。不要假设所有 FreeSql 查询都会自动带上数据权限条件。

## 写领域服务

`Services/ArticlePublishingService.cs`：

```csharp
namespace LimeMetaService.Services;

using LimeMeta.Authorization;
using LimeMeta.Data;
using LimeMetaService.Models;

public sealed class ArticlePublishingService(
    ILimeMeta meta,
    ILimeMetaAuthorizationService authorization)
{
    public bool Publish(Guid articleId, Guid userId)
    {
        authorization.EnsureAuthorized(
            meta,
            userId,
            typeof(Article),
            LimeMetaOperation.Update);

        var article = meta.Query<Article>()
            .FirstOrDefault(x => x.Id == articleId)
            ?? throw new InvalidOperationException("文章不存在。");

        article.Published = true;
        return meta.Update(
            [article],
            [nameof(Article.Published)],
            userId) > 0;
    }
}
```

在 `Extensions.cs` 注册：

```csharp
services.AddScoped<ArticlePublishingService>();
```

领域服务显式调用授权，因为直接使用 `ILimeMeta` 不会自动经过 GraphQL 授权层。

## 扩展 GraphQL Query

`TypeExtensions/ArticleQueries.cs`：

```csharp
namespace LimeMetaService.TypeExtensions;

using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LimeMeta.Authorization;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMetaService.Models;

[ExtendObjectType("Query")]
public sealed class ArticleQueries
{
    public Article? ArticleById(
        Guid id,
        [Service] ILimeMeta meta,
        [Service] ILimeMetaAuthorizationService authorization,
        IResolverContext context)
    {
        var userId = GetUserId(context);
        authorization.EnsureAuthorized(
            meta,
            userId,
            typeof(Article),
            LimeMetaOperation.Query);

        return meta.Query<Article>()
            .Include(x => x.Category)
            .FirstOrDefault(x => x.Id == id);
    }

    private static Guid GetUserId(IResolverContext context)
    {
        var value = context.GetUser()?.Claims
            .FirstOrDefault(x => x.Type == UserLogic.ClaimUserId)?.Value;

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("用户未认证。");
    }
}
```

在 `Extensions.cs` 的 `AddLimeMetaService` 中：

```csharp
using LimeMetaService.TypeExtensions;

gqlBuilder.AddTypeExtension<ArticleQueries>();
```

## 扩展 GraphQL Mutation

`TypeExtensions/ArticleMutations.cs`：

```csharp
namespace LimeMetaService.TypeExtensions;

using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LimeMeta.Logics;
using LimeMetaService.Services;

[ExtendObjectType("Mutation")]
public sealed class ArticleMutations
{
    public bool PublishArticle(
        Guid articleId,
        [Service] ArticlePublishingService service,
        IResolverContext context)
    {
        var value = context.GetUser()?.Claims
            .FirstOrDefault(x => x.Type == UserLogic.ClaimUserId)?.Value;
        var userId = Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("用户未认证。");

        return service.Publish(articleId, userId);
    }
}
```

注册：

```csharp
gqlBuilder.AddTypeExtension<ArticleMutations>();
```

调用：

```graphql
mutation {
  publishArticle(articleId: "文章 ID")
}
```

自定义 GraphQL 扩展不会自动调用 `ILimeMetaAuthorizationService`，所以授权应在 Resolver 或它调用的领域服务中完成。

## 写 FastEndpoints HTTP 接口

在业务项目中新建 `Services/HealthEndpoint.cs`，也可以单独建立 `Endpoints/` 目录：

```csharp
namespace LimeMetaService.Endpoints;

using FastEndpoints;

public sealed class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.OkAsync(new HealthResponse
        {
            Status = "ok",
            Time = DateTimeOffset.UtcNow
        }, ct);
    }
}

public sealed class HealthResponse
{
    public required string Status { get; set; }
    public DateTimeOffset Time { get; set; }
}
```

需要认证的业务接口不要调用 `AllowAnonymous()`：

```csharp
namespace LimeMetaService.Endpoints;

using FastEndpoints;
using LimeMeta.Logics;
using LimeMetaService.Services;

public sealed class PublishArticleEndpoint(
    ArticlePublishingService service)
    : Endpoint<PublishArticleRequest, PublishArticleResponse>
{
    public override void Configure()
    {
        Post("/api/articles/{ArticleId}/publish");
    }

    public override async Task HandleAsync(
        PublishArticleRequest req,
        CancellationToken ct)
    {
        var value = User.Claims
            .FirstOrDefault(x => x.Type == UserLogic.ClaimUserId)?.Value;
        var userId = Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("用户未认证。");

        var changed = service.Publish(req.ArticleId, userId);
        await Send.OkAsync(new PublishArticleResponse
        {
            Changed = changed
        }, ct);
    }
}

public sealed class PublishArticleRequest
{
    public Guid ArticleId { get; set; }
}

public sealed class PublishArticleResponse
{
    public bool Changed { get; set; }
}
```

开发环境启用 Swagger。FastEndpoints 会扫描已加载程序集中的 Endpoint；业务程序集由模板模块注册和项目引用加载。

## 登录事件和 JWT 扩展

框架公开：

- `UserLogic.BeforeLogin`
- `UserLogic.AfterLogin`
- `UserLogic.GeneratingJwt`

`BeforeLogin` 可以设置 `args.Cancel = true`，此时登录立即返回空结果，不再查询用户或验证密码。`AfterLogin` 只在密码校验成功并生成 Token 后触发。

可以在应用启动阶段订阅，例如给 JWT 添加声明：

```csharp
using System.Security.Claims;
using LimeMeta.Logics;

UserLogic.GeneratingJwt += (_, args) =>
{
    args.Options.User.Claims.Add(
        new Claim("service", "LimeMetaService"));
};
```

这些是静态事件。开发环境热重载或测试重复启动时，要避免重复订阅。复杂需求更适合封装为明确的启动注册组件。

## 写 WebSocket 消息处理器

WebSocket 处理器是在框架服务注册阶段从已加载程序集发现的。最稳妥的放置位置是宿主项目 `LimeMetaService.WebAPI/WebSockets/NoticeWsController.cs`：

```csharp
namespace LimeMetaService.WebAPI.WebSockets;

using LimeMeta.WebSockets;

[WsController]
public sealed class NoticeWsController
{
    [WsMessage("notice.ping")]
    public object Ping(
        PingRequest request,
        LimeMetaWebSocketContext context)
    {
        if (context.UserId is null)
        {
            throw new UnauthorizedAccessException("用户未认证。");
        }

        return new
        {
            request.Text,
            context.ConnectionId,
            context.UserId,
            serverTime = DateTimeOffset.UtcNow
        };
    }
}

public sealed class PingRequest
{
    public string? Text { get; set; }
}
```

每个消息方法最多有：

- 一个消息体参数。
- 一个 `LimeMetaWebSocketContext`。
- 一个 `CancellationToken`。

返回值可以是普通对象、`Task` 或 `Task<T>`。

客户端连接：

```text
ws://127.0.0.1:6675/api/ws?access_token=<JWT>
```

发送：

```json
{
  "id": "request-1",
  "type": "notice.ping",
  "data": {
    "text": "hello"
  }
}
```

成功响应：

```json
{
  "id": "request-1",
  "type": "notice.ping.result",
  "success": true,
  "data": {
    "text": "hello"
  }
}
```

失败响应的类型是 `notice.ping.error`，并带 `error` 字段。

WebSocket 入口会解析身份，但当前不会统一拒绝匿名连接。需要登录的消息必须像示例一样检查 `context.UserId`，或在业务层增加统一约束。

主动发送：

```csharp
await context.SendAsync("notice.received", new { ok = true }, ct);
await context.Connections.SendToUserAsync(
    userId,
    "article.published",
    new { articleId },
    ct);
await context.Connections.BroadcastAsync(
    "system.announcement",
    new { text = "系统维护" },
    ct);
```

连接管理器还支持按 `ConnectionId` 单播。广播前要判断内容是否适合所有在线连接，避免跨用户或跨租户泄露。

下一篇：[配置、种子、文件存储与部署](05-configuration-and-deployment.md)。
