using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 用户角色
/// </summary>
[Table(Name = "user_role")]
public class UserRole : BaseObject
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Column(Name = "user_id"), Indexed]
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [Column(Name = "role_id"), Indexed]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [Navigate(nameof(RoleId))]
    public Role? Role { get; set; }
}

/// <summary>
/// UserRoleDto
/// </summary>
public class UserRoleDto : BaseDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }
}
