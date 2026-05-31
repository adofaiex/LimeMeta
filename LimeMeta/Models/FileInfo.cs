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
