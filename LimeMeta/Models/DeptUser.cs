using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LimeMeta.Models;

/// <summary>
/// 部门用户
/// </summary>
[Table(Name = "dept_user")]
public class DeptUser : BaseObject
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
    /// 用户ID
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
/// DeptUserDto
/// </summary>
public class DeptUserDto : BaseDto
{
    /// <summary>
    /// 部门ID
    /// </summary>
    public Guid DeptId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }
}

