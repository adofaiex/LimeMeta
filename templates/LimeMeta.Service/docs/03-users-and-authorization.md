# 用户、角色、权限与安全

## 权限关系先看这一张图

```text
User ── UserRole ── Role ── RolePerm ── Perm
  │                    │
  └── DeptUser ── Dept └── DeptRole ───┘
```

含义：

- 用户可以通过 `UserRole` 直接拥有角色。
- 用户也可以通过 `DeptUser` 加入部门。
- 部门通过 `DeptRole` 拥有角色。
- 角色通过 `RolePerm` 拥有权限。
- `Role`、`Perm`、`Dept` 都是树形模型。

`Perm.Name` 是当前框架判断权限的主要值。建议在业务项目中把权限名集中定义成常量，避免到处手写字符串：

```csharp
namespace LimeMetaService;

public static class ProjectPerms
{
    public const string ArticleRead = "article.read";
    public const string ArticleWrite = "article.write";
    public const string ArticlePublish = "article.publish";
}
```

## 启动时如何初始化管理员

当 `LoadSeedOnStartup` 为 `true` 时，框架会确保存在：

1. 名称等于 `LimeMeta.AdminPerm` 的权限。
2. 同名管理员角色。
3. 管理员角色与管理员权限的关联。
4. 用户名等于 `LimeMeta.AdminUserName` 的管理员用户。
5. 管理员用户与管理员角色的关联。

管理员用户只在不存在时创建。之后修改配置中的 `AdminUserPassword` 不会自动重置数据库中的密码；请使用 `resetUserPassword`，或在受控环境中明确处理。

管理员有两条特殊规则：

- 用户名等于 `AdminUserName` 的用户会获得全部角色和全部权限。
- 任意用户只要最终权限中包含名称等于 `AdminPerm` 的权限，也被视为管理员，并获得全部权限。

因此不要把管理员权限随意分配给普通角色。

## 角色和部门的实际继承规则

当前 `UserLogic.GetRoles` 按以下顺序计算一个用户的角色：

1. 读取用户直接关联的角色。
2. 读取用户直接关联的部门。
3. 加入这些部门及其所有子部门上关联的角色。
4. 对前面得到的每个角色，再加入该角色及其所有子角色。

这意味着：

- 用户在父部门时，会拿到该父部门以及子部门关联的角色。
- 用户拿到一个父角色时，也会拿到其子角色。

这与一些系统采用的“从父部门向下继承给成员”或“子角色继承父角色”并不完全相同。建权限数据前要确认这正是你的业务语义；如果不是，应替换授权服务或编写自己的角色解析服务。

权限的 `ParentId`、`Path` 和 `NamePath` 主要用于组织权限树。当前 `GetPerms` 不会因为拥有一个父权限就自动展开所有子权限；真正授予用户的是 `RolePerm` 明确关联的权限。

## 创建权限、角色和关联

`Perm` 不提供通用 GraphQL Mutation。推荐把稳定的权限定义放进 `Seed/Perm.yaml`，使用固定 ID：

```yaml
- id: "10000000-0000-0000-0000-000000000001"
  name: "article.read"
  sn: 10
  parentId:

- id: "10000000-0000-0000-0000-000000000002"
  name: "article.write"
  sn: 20
  parentId:

- id: "10000000-0000-0000-0000-000000000003"
  name: "article.publish"
  sn: 30
  parentId:
```

Seed 会在管理员初始化后按模型文件名加载。更完整的规则见 [配置、种子、文件存储与部署](05-configuration-and-deployment.md)。

管理员可以使用通用系统模型 Mutation 创建角色：

```graphql
mutation {
  insertRole(
    objs: [{
      name: "内容编辑"
      sn: 10
      parentId: null
    }]
  )
}
```

再创建角色与权限关联：

```graphql
mutation {
  insertRolePerm(
    objs: [{
      roleId: "角色 ID"
      permId: "权限 ID"
    }]
  )
}
```

同理可通过 `insertDept`、`insertDeptUser` 和 `insertDeptRole` 管理组织关系。它们是系统模型操作，默认授权服务只允许管理员调用。

## 登录和 Token

登录是允许匿名调用的 GraphQL Mutation：

```graphql
mutation {
  login(username: "admin", password: "管理员密码") {
    name
    token
  }
}
```

密码在服务端使用 BCrypt 校验。默认工作因子为 12，每次哈希使用独立随机盐，数据库保存的是包含算法参数和盐的完整哈希。

登录失败会返回一个属性为空的结果，不会把“用户名不存在”和“密码错误”区别暴露给客户端。

后续 HTTP/GraphQL 请求：

```text
Authorization: Bearer <token>
```

生产环境必须启用 HTTPS，否则用户名、密码和 Bearer Token 仍可能在传输途中泄露。

JWT 默认只包含 LimeMeta 用户 ID Claim：

```text
meta-user-id
```

过期时间由 `LimeMeta.JwtExpires` 控制，单位为毫秒，默认 `86400000`，即 24 小时。

## 扩展登录返回内容

内置 `login` 的 GraphQL 返回类型固定为：

```text
name
token
```

如果前端登录后还需要用户 ID、头像、部门、角色、权限或业务资料，不建议把这些字段硬塞进框架内置 `User`。更稳妥的做法是：

1. 新建 `UserProfile` 等业务模型，通过 `UserId` 关联内置用户。
2. 新增自己的登录 Mutation。
3. 内部调用 `UserLogic.Login` 复用 BCrypt 校验、JWT 生成和登录事件。
4. 登录成功后查询并组合自己的返回 DTO。

示例：

```csharp
namespace LimeMetaService.TypeExtensions;

using HotChocolate;
using HotChocolate.Types;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;
using LimeMeta.Security;

[ExtendObjectType("Mutation")]
public sealed class AccountMutations
{
    public ProjectLoginResult ProjectLogin(
        string username,
        string password,
        [Service] ILimeMeta meta,
        [Service] ILimeMetaPasswordHasher passwordHasher)
    {
        var login = UserLogic.Login(
            meta,
            passwordHasher,
            username,
            password);

        if (string.IsNullOrWhiteSpace(login.Token))
        {
            return new ProjectLoginResult();
        }

        var user = meta.Query<User>()
            .FirstOrDefault(x => x.Username == username)
            ?? throw new InvalidOperationException("登录用户不存在。");
        var roles = UserLogic.GetRoles(meta, user.Id)
            .Select(x => x.Name)
            .ToArray();
        var permissions = UserLogic.GetPerms(meta, user.Id)
            .Select(x => x.Name)
            .ToArray();

        return new ProjectLoginResult
        {
            UserId = user.Id,
            Name = user.Name,
            Token = login.Token,
            Roles = roles,
            Permissions = permissions
        };
    }
}

public sealed class ProjectLoginResult
{
    public Guid? UserId { get; set; }
    public string? Name { get; set; }
    public string? Token { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<string> Permissions { get; set; } = [];
}
```

在 `Extensions.cs` 注册：

```csharp
gqlBuilder.AddTypeExtension<AccountMutations>();
```

客户端改为调用：

```graphql
mutation {
  projectLogin(username: "admin", password: "管理员密码") {
    userId
    name
    token
    roles
    permissions
  }
}
```

如果只是想在 JWT 中增加租户、客户端类型等 Claim，可以订阅 `UserLogic.GeneratingJwt`，不必重写登录。登录前风控可在 `BeforeLogin` 中设置 `Cancel = true`；取消后不会查询用户或校验密码。`AfterLogin` 只在登录成功后触发，适合记录最后登录时间和安全审计。

这些登录事件是静态事件。应用启动和测试中要避免重复订阅，并且不能在日志中记录事件参数里的明文密码。对公网登录接口还应配置 HTTPS、限流、失败次数控制和安全审计。

## 专用用户 Mutation

### 创建用户

只有管理员可调用：

```graphql
mutation {
  createUser(
    name: "张三"
    username: "zhangsan"
    password: "a-strong-initial-password"
    phone: "13800000000"
    roleIds: ["角色 ID"]
  )
}
```

返回新用户 ID。框架会在服务端哈希密码，并验证角色 ID 是否存在。

如果没有传入角色，而数据库中存在名称为 `游客` 的角色，`UserLogic` 会自动给新用户分配该角色。模板不会凭空创建游客角色；如果需要它，请用 Seed 明确定义。

### 更新用户资料和角色

只有管理员可调用：

```graphql
mutation {
  updateUser(
    userId: "用户 ID"
    name: "张三（编辑）"
    phone: null
    roleIds: ["角色 ID 1", "角色 ID 2"]
  )
}
```

参数语义：

- `name` 未提供或为空白：保留原值。
- `phone` 未提供或传 `null`：保留原值；传空字符串：清空。
- `roleIds` 未提供：保留现有角色；传空数组：清空角色。

用户名不能通过这个 Mutation 修改。

### 用户修改自己的密码

```graphql
mutation {
  changePassword(
    currentPassword: "old-password"
    newPassword: "new-strong-password"
  )
}
```

必须正确提供当前密码。

### 管理员重置用户密码

```graphql
mutation {
  resetUserPassword(
    userId: "用户 ID"
    newPassword: "new-strong-password"
  )
}
```

只有管理员可调用。

### 删除用户

```graphql
mutation {
  deleteUser(userId: "用户 ID")
}
```

只有管理员可调用，且管理员不能删除当前登录账号。删除用户时会清理 `DeptUser` 和 `UserRole` 关系。

## 查询当前用户的身份数据

```graphql
query {
  currUserId
  currUserRole {
    id
    name
  }
  currUserPerm {
    id
    name
  }
}
```

框架还提供：

- `allUserRole(userId)`
- `allUserPerm(userId)`
- `allDeptRole(deptId)`
- `allDeptPerm(deptId)`

这些查询要求已认证。默认策略没有进一步限制“能否查看他人的角色权限”，如果这在你的项目中属于敏感信息，应写自定义 Query 或更严格的授权层。

## 默认模型授权策略

自动生成的 GraphQL Query 和 Mutation 都会调用：

```csharp
ILimeMetaAuthorizationService.EnsureAuthorized(
    ILimeMeta meta,
    Guid userId,
    Type modelType,
    LimeMetaOperation operation);
```

操作包括：

- `Query`
- `Aggregate`
- `Insert`
- `Update`
- `Delete`

默认规则：

| 模型 | Query / Aggregate | Insert / Update / Delete |
| --- | --- | --- |
| 业务模型 | 已认证即可 | 已认证即可 |
| LimeMeta 内置系统模型 | 已认证即可 | 仅管理员 |

这是能启动开发的基线，不是大多数生产系统的最终权限设计。

## 替换为业务授权策略

在 `LimeMetaService/Services/ProjectAuthorizationService.cs`：

```csharp
namespace LimeMetaService.Services;

using LimeMeta.Authorization;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;
using LimeMetaService.Models;

public sealed class ProjectAuthorizationService : ILimeMetaAuthorizationService
{
    public void EnsureAuthorized(
        ILimeMeta meta,
        Guid userId,
        Type modelType,
        LimeMetaOperation operation)
    {
        if (UserLogic.IsAdmin(meta, userId))
        {
            return;
        }

        var permissionNames = UserLogic.GetPerms(meta, userId)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (modelType == typeof(Article))
        {
            var required = operation switch
            {
                LimeMetaOperation.Query or LimeMetaOperation.Aggregate
                    => ProjectPerms.ArticleRead,
                LimeMetaOperation.Insert or LimeMetaOperation.Update
                    => ProjectPerms.ArticleWrite,
                LimeMetaOperation.Delete
                    => ProjectPerms.ArticlePublish,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };

            if (!permissionNames.Contains(required))
            {
                throw new UnauthorizedAccessException($"缺少权限：{required}");
            }

            return;
        }

        var isSystemModel = modelType.Assembly == typeof(User).Assembly;
        if (isSystemModel &&
            operation is LimeMetaOperation.Insert
                or LimeMetaOperation.Update
                or LimeMetaOperation.Delete)
        {
            throw new UnauthorizedAccessException("只有管理员可以修改系统模型。");
        }
    }
}
```

在模板的 `Extensions.cs` 中注册：

```csharp
using LimeMeta.Authorization;
using LimeMetaService.Services;

services.AddScoped<ILimeMetaAuthorizationService, ProjectAuthorizationService>();
```

模板先注册框架默认实现，再调用 `AddLimeMetaService`；后注册的业务实现会成为单服务解析结果。

这个接口只能决定“整个操作允许还是拒绝”，不能改写查询来实现逐行数据权限。比如“编辑只能看到自己创建的文章”，应在 `ArticleLogic.BeforeSelect` 中追加条件，并在 Update/Delete 的专用接口或 Logic 中再次校验对象归属。

## 自定义接口必须主动授权

`ILimeMetaAuthorizationService` 自动保护的是 LimeMeta 自动生成的模型 GraphQL 操作。你自己写的：

- GraphQL Type Extension
- FastEndpoints Endpoint
- 领域服务
- WebSocket 消息处理器

不会自动经过这个接口。它们必须显式验证用户、权限和资源归属。

## AppKey

AppKey 可以代表某个用户调用普通 HTTP/GraphQL 接口：

```text
x-limemeta-app-key: <guid>
```

框架查询 `AppKey.User`，如果存在且未过期，就为该用户生成 JWT 身份。`Expired` 使用 Unix 毫秒时间戳；负数表示永不过期。

普通 HTTP 不接受查询字符串中的 AppKey。不要把 AppKey 放在 URL。

`AppKeyDto` 不接受客户端提交 Key。需要创建 AppKey 时，应写管理员专用接口，在服务端使用 `Guid.NewGuid()` 生成并只展示一次：

```csharp
var appKey = new AppKey
{
    Name = "报表任务",
    Key = Guid.NewGuid(),
    UserId = userId,
    Expired = DateTimeOffset.UtcNow
        .AddDays(30)
        .ToUnixTimeMilliseconds()
};
```

数据库泄露会使仍有效的 AppKey 可被直接使用，因此应支持撤销、轮换，并避免创建永久 Key。

## 替换密码哈希实现

如有合规要求，可以实现：

```csharp
ILimeMetaPasswordHasher
```

并在 `Extensions.cs` 注册自己的实现：

```csharp
services.AddSingleton<ILimeMetaPasswordHasher, ProjectPasswordHasher>();
```

实现必须：

- 每个密码使用独立随机盐。
- 哈希结果包含验证所需参数。
- 使用专门的慢密码哈希算法。
- 不能记录明文密码或哈希。

下一篇：[Logic、HTTP 接口、GraphQL 扩展与 WebSocket](04-logic-and-extensions.md)。
