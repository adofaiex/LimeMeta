using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 基础父级子级泛型
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseParentChildren<T> : BaseAudit, IParentChildren where T : BaseObject
{
    /// <summary>
    /// 父级ID
    /// </summary>
    [Column(Name = "parent_id"), Indexed]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 父级
    /// </summary>
    [Navigate(nameof(ParentId))]
    public T? Parent { get; set; }

    /// <summary>
    /// 孩子
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<T> Children { get; set; } = [];

    /// <summary>
    /// 路径
    /// </summary>
    [Column(Name = "path"), Indexed]
    public string? Path { get; set; }

    /// <summary>
    /// 名称路径
    /// </summary>
    [Column(Name = "name_path"), Indexed]
    public string? NamePath { get; set; }
}

/// <summary>
/// 基础父级子级DTO
/// </summary>
public abstract class BaseParentChildrenDto : BaseDto
{
    /// <summary>
    /// 父ID
    /// </summary>
    public Guid? ParentId { get; set; }
}
