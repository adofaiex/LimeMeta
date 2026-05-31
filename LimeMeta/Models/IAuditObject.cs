using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimeMeta.Models;
/// <summary>
/// 审计对象接口
/// </summary>
public interface IAuditObject : IBaseObject
{
    /// <summary>
    /// 创建时间
    /// </summary>
    long Created { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    Guid? CreatorId { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    long Updated { get; set; }

    /// <summary>
    /// 修改者ID
    /// </summary>
    Guid? ModifierId { get; set; }
}
