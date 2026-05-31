namespace LimeMeta.Configurations;

using FreeSql;

/// <summary>
/// LimeMeta 的配置项，支持从 appsettings（含 YAML）绑定。
/// </summary>
public sealed class LimeMetaConfiguration
{
    /// <summary>
    /// FreeSql 连接字符串。
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// FreeSql 数据库类型，默认为 Sqlite。
    /// </summary>
    public DataType DataType { get; set; } = DataType.Sqlite;

    /// <summary>
    /// 管理员权限
    /// </summary>
    public string AdminPerm { get; set; } = "管理员";

    /// <summary>
    /// 来宾权限
    /// </summary>
    public string GuestPerm { get; set; } = "游客";

    /// <summary>
    /// 管理员
    /// </summary>
    public string AdminUserName { get; set; } = "admin";

    /// <summary>
    /// 管理员密码
    /// </summary>
    public string AdminUserPassword { get; set; } = "change-me-admin-password";

    /// <summary>
    /// 默认用户密码
    /// </summary>
    public string DefaultUserPassword { get; set; } = "change-me-user-password";

    /// <summary>
    /// 加密盐
    /// </summary>
    public string Salt { get; set; } = "$2a$12$v76VCF8eVvsTeLLTJ1Gu3O";

    /// <summary>
    /// Jwt 签名密钥
    /// </summary>
    public string JwtSignKey { get; set; } = "2dd58a6c19b7416e8aa7dbe72441ba1bdb93749b14364fadbeda259f8ec66640";

    /// <summary>
    /// Jwt 过期时间
    /// </summary>
    public long JwtExpires { get; set; } = 86400000;

    /// <summary>
    /// 文件存储路径
    /// </summary>
    public string FileStorePath { get; set; } = "FileStore";

    /// <summary>
    /// 文件存储数量
    /// </summary>
    public int FileStoreCount { get; set; } = 8192;
}

