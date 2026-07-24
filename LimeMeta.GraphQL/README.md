# LimeMeta.GraphQL

LimeMeta.GraphQL 基于 HotChocolate，为 LimeMeta 模型自动生成带认证和授权检查的分页查询、过滤、排序、聚合以及增删改 Mutation。

推荐通过项目模板使用：

```powershell
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
```

完整文档与源码：[github.com/memsys-lizi/LimeMeta](https://github.com/memsys-lizi/LimeMeta)

查询、聚合、增删改、专用用户 Mutation、自定义 Type Extension 和授权示例见[模型与 GraphQL 文档](https://github.com/memsys-lizi/LimeMeta/blob/main/templates/LimeMeta.Service/docs/02-models-and-graphql.md)及[用户权限文档](https://github.com/memsys-lizi/LimeMeta/blob/main/templates/LimeMeta.Service/docs/03-users-and-authorization.md)。
