using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 角色
/// </summary>
[Table(Name = "role")]
public class Role : BaseParentChildren<Role>
{
    /// <summary>
    /// 名称
    /// </summary>
    [Column(Name = "name"), Indexed]
    public required string Name { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    [Column(Name = "sn"), Indexed]
    public int Sn { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(ManyToMany = typeof(UserRole))]
    public List<User> Users { get; set; } = [];

    /// <summary>
    /// 权限
    /// </summary>
    [Navigate(ManyToMany = typeof(RolePerm))]
    public List<Perm> Perms { get; set; } = [];
}

/// <summary>
/// RoleDto
/// </summary>
public class RoleDto : BaseParentChildrenDto
{
    /// <summary>
    /// 名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    public int Sn { get; set; } = 0;
}

