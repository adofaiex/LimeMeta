using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// FileInfo
/// </summary>
[Table(Name = "file_info")]
public class FileInfo : BaseAudit
{
    /// <summary>
    /// 文件存储服务
    /// </summary>
    [Column(Name = "provider"), Indexed]
    public string? Provider { get; set; } = "Local";

    /// <summary>
    /// 存储位置
    /// </summary>
    [Column(Name = "store"), Indexed]
    public int Store { get; set; } = 0;

    /// <summary>
    /// 文件名
    /// </summary>
    [Column(Name = "name"), Indexed]
    public required string Name { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    [Column(Name = "real"), Indexed]
    public required string Real { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    [Column(Name = "type"), Indexed]
    public string? Type { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    [Column(Name = "size"), Indexed]
    public required long Size { get; set; }

    /// <summary>
    /// 文件哈希
    /// </summary>
    [Column(Name = "hash"), Indexed]
    public required string Hash { get; set; }

    /// <summary>
    /// 第三方存储文件 ID
    /// </summary>
    [Column(Name = "provider_id"), Indexed]
    public string? ProviderId { get; set; }

    /// <summary>
    /// 第三方存储路径或父目录 ID
    /// </summary>
    [Column(Name = "provider_path"), Indexed]
    public string? ProviderPath { get; set; }

    /// <summary>
    /// 外部访问地址
    /// </summary>
    [Column(Name = "url")]
    public string? Url { get; set; }

    /// <summary>
    /// 第三方存储返回的原始信息
    /// </summary>
    [Column(Name = "meta")]
    public string? Meta { get; set; }
}

/// <summary>
/// FileInfoDto
/// </summary>
public class FileInfoDto : BaseDto
{
    /// <summary>
    /// 文件名
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public string? Type { get; set; }
}
