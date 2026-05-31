using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimeMeta.Logics;

/// <summary>
/// 逻辑接口
/// </summary>
public interface ILogic
{
    /// <summary>
    /// 序号
    /// </summary>
    float Order { get; }

    /// <summary>
    /// 模型类型
    /// </summary>
    /// <value></value>
    Type LogicModelType { get; }
}
