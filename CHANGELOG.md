# 更新日志

本项目遵循 [Semantic Versioning](https://semver.org/)。

## [Unreleased]

## [3.1.0] - 2026-07-28

### Added

- `[LimeMetaAuthorize]` 的 `Read`、`Create`、`Update` 和 `Delete` 支持使用 `|` 声明多个备选权限；用户拥有其中任意一个即可执行对应操作。
- 启动检查会拒绝包含空备选项的权限声明，避免 `权限A||权限B` 等拼写错误静默导致授权异常。

## [3.0.0] - 2026-07-28

### Added

- 新增模型级 `[LimeMetaAuthorize("权限前缀")]`，自动将查询/聚合、新增、编辑、删除分别映射到读取权限及默认的 `.新增`、`.编辑`、`.删除` 权限。
- 新增 `[LimeMetaAllowAuthenticated]`，用于明确放行指定操作给任意已登录用户；未放行的操作仍仅管理员可执行。

### Changed

- 业务模型必须显式选择 `[LimeMetaAuthorize]`、`[LimeMetaAllowAuthenticated]` 或 `[DisableGraphQL]` 之一，否则应用启动时直接报错，不再默认允许所有已登录用户操作。
- 管理员仍可执行所有自动 GraphQL 操作；LimeMeta 内置系统模型继续保持“已登录可查询、仅管理员可修改”的规则。
- 这是安全默认值和模型声明方式的破坏性变化；已有源码项目升级后需要为每个业务模型补充访问策略。

## [2.1.0] - 2026-07-27

### Added

- 新增模型级 `[DisableGraphQL]`，允许模型继续使用数据库同步、Seed、Logic 和 `ILimeMeta`，同时关闭自动 GraphQL 查询、聚合和 Mutation。

## [2.0.0] - 2026-07-26

### Changed

- 停止构建和发布 `LimeMeta`、`LimeMeta.GraphQL` 框架 NuGet 包。
- `LimeMeta.Templates` 生成的解决方案改为直接包含两个框架源码项目，并通过 `ProjectReference` 使用。
- 模板生成结构由两个项目调整为四个项目；生成后的框架源码作为独立快照由业务项目自行维护。
- 发布工作流改为只验证和发布源码内置模板包。

## [1.0.1] - 2026-07-25

### Fixed

- 更新文档示例与安装说明，统一 `dotnet new install` 写法。

## [1.0.0] - 已发布

### Added

- 模型驱动的 FreeSql 数据层、结构同步和种子数据。
- 自动 GraphQL 查询、聚合与 Mutation。
- Logic 生命周期、JWT、AppKey、文件存储与 WebSocket。
- MySQL 默认项目模板和 PostgreSQL 支持。
- 模板内完整中文开发手册，覆盖模型、GraphQL、用户权限、Logic、接口、配置与部署。
- NuGet.org 公开发布（`LimeMeta`、`LimeMeta.GraphQL`、`LimeMeta.Templates`）、SourceLink、符号包和自动发布流程。

### Fixed

- `BeforeLoginEventArgs.Cancel` 现在会真正中止登录，不再继续查询用户或校验密码。
- Local 文件存储默认路径统一为当前目录下的 `./FileStore`。

### Security

- 密码改为服务端 BCrypt 独立随机盐。
- 生产环境强制显式配置数据库、管理员密码与 JWT 密钥。
- 增加模型授权扩展点并保护系统模型修改。

