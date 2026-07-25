# 更新日志

本项目遵循 [Semantic Versioning](https://semver.org/)。

## [Unreleased]

## [1.1.0] - 2026-07-25

### Added

- 文件存储公开 URL：`IFileStorageProvider.ResolvePublicUrlAsync`、`FileUrlResolver`；Pan123 上传写入直链；上传响应返回 `url`；下载支持 `redirect=false`。
- 内置用户字段：`User.Email`、`User.AvatarFileId`；`LoginResult` 增加 `userId`/`email`/`avatarUrl`。
- 内置 SMTP（`LimeMeta:Smtp`）与邮箱验证码；匿名 `sendRegisterCode` / `register`；登录态 `updateMyAvatar`。
- `[LimeMetaIgnoreGraphQL]`：模型参与建表但不生成自动 GraphQL CRUD。
- 配置：`AllowSelfRegister`、`RegisterRequireEmailCode`。

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

