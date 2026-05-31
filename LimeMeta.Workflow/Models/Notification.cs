using System;
using LimeMeta.Models;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Workflow.Models;

/// <summary>
/// Notification - 应用内通知
/// </summary>
[Table(Name = "notification")]
public class Notification : BaseAudit
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Column(Name = "user_id"), Indexed]
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [Column(Name = "title", StringLength = 200)]
    public required string Title { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    [Column(Name = "content", StringLength = -1)]
    public required string Content { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [Column(Name = "is_read"), Indexed]
    public bool IsRead { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [Column(Name = "link", StringLength = 500)]
    public string? Link { get; set; }

    /// <summary>
    /// 通知类型（workflow/reminder等）
    /// </summary>
    [Column(Name = "type", StringLength = 50)]
    public string? Type { get; set; }
}

/// <summary>
/// NotificationDto
/// </summary>
public class NotificationDto : BaseDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public string? Type { get; set; }
}

