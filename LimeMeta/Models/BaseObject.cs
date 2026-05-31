using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 基础对象
/// </summary>
public abstract class BaseObject : IBaseObject
{
    /// <summary>
    /// ID
    /// </summary>
    [Column(Name = "id", IsPrimary = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Version
    /// </summary>
    [Column(Name = "_ver"), Indexed]
    public long Ver { get; set; } = DateTime.Now.ToReadableLong();

    /// <summary>
    /// 克隆
    /// </summary>
    /// <returns></returns>
    public object Clone() => MemberwiseClone();
}

/// <summary>
/// BaseDto
/// </summary>
public abstract class BaseDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

