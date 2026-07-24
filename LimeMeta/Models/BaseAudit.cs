using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 基础审计
/// </summary>
public abstract class BaseAudit : BaseObject, IAuditObject
{
    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(Name = "created"), Indexed]
    public long Created { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [Column(Name = "creator_id"), Indexed]
    public Guid? CreatorId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [Navigate(nameof(CreatorId))]
    public User? Creator { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Column(Name = "updated"), Indexed]
    public long Updated { get; set; }

    /// <summary>
    /// 修改者ID
    /// </summary>
    [Column(Name = "modifier_id"), Indexed]
    public Guid? ModifierId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    [Navigate(nameof(ModifierId))]
    public User? Modifier { get; set; }
}
