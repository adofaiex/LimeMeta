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
    public string ConnectionString { get; set; } = string.Empty;

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
    public string AdminUserPassword { get; set; } = string.Empty;

    /// <summary>
    /// 启动时是否自动同步数据库表结构
    /// </summary>
    public bool AutoSyncSchema { get; set; } = true;

    /// <summary>
    /// 启动时是否自动加载种子数据
    /// </summary>
    public bool LoadSeedOnStartup { get; set; } = true;

    /// <summary>
    /// Jwt 签名密钥
    /// </summary>
    public string JwtSignKey { get; set; } = string.Empty;

    /// <summary>
    /// Jwt 过期时间
    /// </summary>
    public long JwtExpires { get; set; } = 86400000;

    /// <summary>
    /// 文件存储路径
    /// </summary>
    public string FileStorePath { get; set; } = "./FileStore";

    /// <summary>
    /// 文件存储数量
    /// </summary>
    public int FileStoreCount { get; set; } = 8192;

    /// <summary>
    /// 文件存储配置
    /// </summary>
    public FileStoreConfiguration FileStore { get; set; } = new();
}

/// <summary>
/// 文件存储配置
/// </summary>
public sealed class FileStoreConfiguration
{
    /// <summary>
    /// 当前文件存储服务。Local 或 Pan123Cli。
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// 本地文件存储配置
    /// </summary>
    public LocalFileStoreConfiguration Local { get; set; } = new();

    /// <summary>
    /// 123 云盘 CLI 文件存储配置
    /// </summary>
    public Pan123CliFileStoreConfiguration Pan123Cli { get; set; } = new();
}

/// <summary>
/// 本地文件存储配置
/// </summary>
public sealed class LocalFileStoreConfiguration
{
    /// <summary>
    /// 本地文件保存根目录。为空时使用旧配置 LimeMeta:FileStorePath。
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 每个本地子目录最多保存多少个文件。为空时使用旧配置 LimeMeta:FileStoreCount。
    /// </summary>
    public int? Count { get; set; }
}

/// <summary>
/// 123 云盘 CLI 文件存储配置
/// </summary>
public sealed class Pan123CliFileStoreConfiguration
{
    /// <summary>
    /// pan123 命令路径。可以是 pan123，也可以是绝对路径。
    /// </summary>
    public string Command { get; set; } = "pan123";

    /// <summary>
    /// 上传到 123 云盘的父目录 ID。
    /// </summary>
    public long ParentFileId { get; set; } = 0;

    /// <summary>
    /// 下载时是否优先使用直链跳转。
    /// </summary>
    public bool UseDirectLink { get; set; } = true;

    /// <summary>
    /// 临时文件目录。上传到 123 云盘前会先把 HTTP 上传流写入这里。
    /// </summary>
    public string TempPath { get; set; } = "./TempUpload";

    /// <summary>
    /// 上传同名文件时是否覆盖。
    /// </summary>
    public bool Overwrite { get; set; } = false;
}

