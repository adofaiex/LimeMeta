# LimeMeta.GraphQL

LimeMeta.GraphQL 基于 HotChocolate，为 LimeMeta 模型自动生成带认证和授权检查的分页查询、过滤、排序、聚合以及增删改 Mutation。

推荐通过项目模板使用：

```powershell
$env:NuGetPackageSourceCredentials_adofaiex = "Username=<GitHub 用户名>;Password=<具有 read:packages 权限的 classic PAT>"
dotnet nuget add source "https://nuget.pkg.github.com/adofaiex/index.json" --name adofaiex
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
```

包与源码仅供组织授权成员使用。不要将 PAT 写入仓库或 `NuGet.config`。

完整文档与源码：[github.com/adofaiex/LimeMeta](https://github.com/adofaiex/LimeMeta)

查询、聚合、增删改、专用用户 Mutation、自定义 Type Extension 和授权示例见[模型与 GraphQL 文档](https://github.com/adofaiex/LimeMeta/blob/main/templates/LimeMeta.Service/docs/02-models-and-graphql.md)及[用户权限文档](https://github.com/adofaiex/LimeMeta/blob/main/templates/LimeMeta.Service/docs/03-users-and-authorization.md)。
