using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 角色权限
/// </summary>
[Table(Name = "role_perm")]
public class RolePerm : BaseObject
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [Column(Name = "role_id"), Indexed]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    [Column(Name = "perm_id"), Indexed]
    public Guid PermId { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [Navigate(nameof(RoleId))]
    public Role? Role { get; set; }

    /// <summary>
    /// 权限
    /// </summary>
    [Navigate(nameof(PermId))]
    public Perm? Perm { get; set; }
}

/// <summary>
/// RolePermDto
/// </summary>
public class RolePermDto : BaseDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public Guid PermId { get; set; }
}
