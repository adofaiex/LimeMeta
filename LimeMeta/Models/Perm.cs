using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 权限
/// </summary>
[Table(Name = "perm")]
public class Perm : BaseParentChildren<Perm>
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
}

/// <summary>
/// PermDto
/// </summary>
public class PermDto : BaseParentChildrenDto
{
    /// <summary>
    /// 名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    public int Sn { get; set; }
}
