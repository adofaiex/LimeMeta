using System;
using System.Collections.Generic;
using System.Linq;

namespace LimeMeta;

/// <summary>
/// AuditAction
/// </summary>
public static class AuditAction
{
    /// <summary>
    /// 插入
    /// </summary>
    public const string Insert = "insert";

    /// <summary>
    /// 更新
    /// </summary>
    public const string Update = "update";

    /// <summary>
    /// 删除
    /// </summary>
    public const string Delete = "delete";
}

/// <summary>
/// RoleName
/// </summary>
public static class RoleName
{
    public const string Admin = "管理员";
    public const string Guest = "游客";
}

