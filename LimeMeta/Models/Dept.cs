using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LimeMeta.Models;

/// <summary>
/// 部门
/// </summary>
[Table(Name = "dept")]
public class Dept : BaseParentChildren<Dept>
{
    /// <summary>
    /// 序号
    /// </summary>
    [Column(Name = "sn"), Indexed]
    public int Sn { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [Column(Name = "name"), Indexed]
    public string? Name { get; set; }

    /// <summary>
    /// 别名
    /// </summary>
    [Column(Name = "alias"), Indexed]
    public string? Alias { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    [Column(Name = "phone"), Indexed]
    public string? Phone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [Column(Name = "address"), Indexed]
    public string? Address { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    [Column(Name = "email"), Indexed]
    public string? Email { get; set; }

    /// <summary>
    /// Avatar
    /// </summary>
    [Column(Name = "avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Column(Name = "note", StringLength = -1)]
    public string? Note { get; set; }

    /// <summary>
    /// 部门用户
    /// </summary>
    [Navigate(ManyToMany = typeof(DeptUser))]
    public List<User> Users { get; set; } = [];

    /// <summary>
    /// 部门角色
    /// </summary>
    [Navigate(ManyToMany = typeof(DeptRole))]
    public List<Role> Roles { get; set; } = [];
}

/// <summary>
/// DeptDto
/// </summary>
public class DeptDto : BaseParentChildrenDto
{
    /// <summary>
    /// 序号
    /// </summary>
    public int Sn { get; set; }

    /// <summary>
    /// 简称
    /// </summary>    
    public string? Name { get; set; }

    /// <summary>
    /// 别名
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; set; }
}

