using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimeMeta.Models;

/// <summary>
/// 父级子级接口
/// </summary>
public interface IParentChildren : IBaseObject
{
    /// <summary>
    /// 父级ID
    /// </summary>
    Guid? ParentId { get; set; }

    /// <summary>
    /// 路径
    /// </summary>
    string? Path { get; set; }

    /// <summary>
    /// 名称路径
    /// </summary>
    string? NamePath { get; set; }
}
