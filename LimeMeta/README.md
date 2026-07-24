# LimeMeta

LimeMeta 是一个面向 .NET 10 的模型驱动后端框架，提供 FreeSql 数据访问、数据库结构同步、Logic 生命周期、JWT 认证、FastEndpoints、文件存储和 WebSocket。

通常不需要单独安装此包。推荐安装项目模板：

```powershell
dotnet new install LimeMeta.Templates
dotnet new limemeta -n MyService
```

完整文档与源码：[github.com/memsys-lizi/LimeMeta](https://github.com/memsys-lizi/LimeMeta)

生成项目内的文档不仅说明安装，也系统介绍内置用户/角色/权限、模型和 DTO 约定、`ILimeMeta`、Logic 生命周期、授权扩展、FastEndpoints、文件存储与 WebSocket。可在线阅读[模板开发指南](https://github.com/memsys-lizi/LimeMeta/blob/main/templates/LimeMeta.Service/README.md)。
