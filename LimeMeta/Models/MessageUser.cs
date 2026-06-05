using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 消息用户
/// </summary>
[Table(Name = "msg_user")]
public class MessageUser : BaseAudit
{
    /// <summary>
    /// MessageId
    /// </summary>
    [Column(Name = "msg_id"), Indexed]
    public Guid MessageId { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    [Navigate(nameof(MessageId))]
    public Message? Message { get; set; }

    /// <summary>
    /// UserId
    /// </summary>
    [Column(Name = "user_id"), Indexed]
    public Guid UserId { get; set; }

    /// <summary>
    /// User
    /// </summary>
    [Navigate(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 已读
    /// </summary>
    [Column(Name = "read"), Indexed]
    public bool Read { get; set; }
}

/// <summary>
/// MessageUserDto
/// </summary>
public class MessageUserDto : BaseDto
{
    /// <summary>
    /// MessageId
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// UserId
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 已读
    /// </summary>
    public bool Read { get; set; }
}
