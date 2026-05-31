using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimeMeta.Models;

/// <summary>
/// App Key
/// </summary>
[Table(Name = "app_key")]
public class AppKey : BaseAudit
{
    /// <summary>
    /// 名称
    /// </summary>
    [Column(Name = "name"), Indexed]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Key
    /// </summary>
    [Column(Name = "key"), Indexed]
    public Guid Key { get; set; } = Guid.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    [Column(Name = "expired"), Indexed]
    public long Expired { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [Column(Name = "user_id"), Indexed]
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(nameof(UserId))]
    public User? User { get; set; }
}


/// <summary>
/// AppKeyDto
/// </summary>
public class AppKeyDto : BaseDto
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public long Expired { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    public Guid UserId { get; set; }
}

