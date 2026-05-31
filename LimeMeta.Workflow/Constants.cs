using System;
using System.Collections.Generic;
using System.Linq;

namespace LimeMeta.Workflow;

/// <summary>
/// FormDataSignType
/// </summary>
public static class FormDataSignType
{
    /// <summary>
    /// 定签
    /// </summary>
    public const string FixedSign = "定签";

    /// <summary>
    /// 或签
    /// </summary>
    public const string OptionalSign = "或签";

    /// <summary>
    /// 会签
    /// </summary>
    public const string ConcurrentSign = "会签";
}

/// <summary>
/// FormDataState
/// </summary>
public static class FormDataState
{
    /// <summary>
    /// 已提交
    /// </summary>
    public const string Submitted = "已提交";

    /// <summary>
    /// 完成
    /// </summary>
    public const string Completed = "完成";
}

/// <summary>
/// FormDataSignState
/// </summary>
public static class FormDataSignState
{
    /// <summary>
    /// 待签核
    /// </summary>
    public const string Pending = "待签核";

    /// <summary>
    /// 同意
    /// </summary>
    public const string Agree = "同意";

    /// <summary>
    /// 拒绝
    /// </summary>
    public const string Refuse = "拒绝";
}
