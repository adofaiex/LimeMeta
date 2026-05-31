using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimeMeta.Models;

/// <summary>
/// 基础对象接口
/// </summary>
public interface IBaseObject : ICloneable
{
    /// <summary>
    /// ID
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Version
    /// </summary>
    long Ver { get; }
}
