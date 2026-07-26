# LimeMeta.Templates

LimeMeta.Templates 用于创建 .NET 10 模型驱动后端项目。生成结果直接包含 `LimeMeta` 和 `LimeMeta.GraphQL` 的完整可编译源码，以及业务项目、WebAPI 宿主和中文开发手册。

## 安装

```powershell
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
cd MyService
dotnet run --project MyService.WebAPI
```

模板默认使用 MySQL。运行前请修改 `MyService.WebAPI/appsettings.Development.yml` 中的连接串。

生成的解决方案包含四个项目：

```text
LimeMeta
LimeMeta.GraphQL
MyService
MyService.WebAPI
```

业务项目使用 `ProjectReference` 引用两个框架源码项目，不依赖 LimeMeta 框架 NuGet 包。生成后的源码快照归当前项目所有，可以直接修改，不会随模板更新自动覆盖。

更新或卸载模板：

```powershell
dotnet new update
dotnet new uninstall LimeMeta.Templates
```

完整开发说明位于生成项目的 `README.md` 和 `docs/`。
