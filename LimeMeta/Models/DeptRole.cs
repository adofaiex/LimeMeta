using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimeMeta.Models;

/// <summary>
/// 部门角色
/// </summary>
[Table(Name = "dept_role")]
public class DeptRole : BaseObject
{
    /// <summary>
    /// 部门ID
    /// </summary>
    [Column(Name = "dept_id"), Indexed]
    public Guid DeptId { get; set; }

    /// <summary>
    /// 部门
    /// </summary>
    [Navigate(nameof(DeptId))]
    public Dept? Dept { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [Column(Name = "role_id"), Indexed]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [Navigate(nameof(RoleId))]
    public Role? Role { get; set; }
}

/// <summary>
/// DeptRoleDto
/// </summary>
public class DeptRoleDto : BaseDto
{
    /// <summary>
    /// 部门ID
    /// </summary>
    public Guid DeptId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }
}

