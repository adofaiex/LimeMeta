using System;

namespace LimeMeta.Attributes;

/// <summary>
/// 明确允许任意已登录用户执行指定的自动 GraphQL 模型操作。
/// 未允许的操作仅管理员可以执行。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class LimeMetaAllowAuthenticatedAttribute : Attribute
{
    /// <summary>
    /// 允许查询和聚合。
    /// </summary>
    public bool Read { get; set; }

    /// <summary>
    /// 允许新增。
    /// </summary>
    public bool Create { get; set; }

    /// <summary>
    /// 允许修改。
    /// </summary>
    public bool Update { get; set; }

    /// <summary>
    /// 允许删除。
    /// </summary>
    public bool Delete { get; set; }
}
