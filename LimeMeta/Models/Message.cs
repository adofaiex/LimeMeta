using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 消息
/// </summary>
[Table(Name = "msg")]
public class Message : BaseAudit
{
    /// <summary>
    /// 标题
    /// </summary>
    [Column(Name = "title"), Indexed]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    [Column(Name = "content", StringLength = -1)]
    public string? Content { get; set; }

    /// <summary>
    /// 模型
    /// </summary>
    [Column(Name = "model"), Indexed]
    public string? Model { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    [Column(Name = "src_id"), Indexed]
    public Guid? SourceId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(ManyToMany = typeof(MessageUser))]
    public List<User> Users { get; set; } = [];
}

/// <summary>
/// MessageDto
/// </summary>
public class MessageDto : BaseDto
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    public Guid? SourceId { get; set; }
}
