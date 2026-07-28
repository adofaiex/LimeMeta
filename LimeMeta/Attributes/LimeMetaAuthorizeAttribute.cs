using System;

namespace LimeMeta.Attributes;

/// <summary>
/// 声明自动 GraphQL 模型各类操作所需的权限名称。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class LimeMetaAuthorizeAttribute : Attribute
{
    /// <summary>
    /// 使用权限前缀和默认操作后缀创建模型权限声明。
    /// </summary>
    /// <param name="permissionPrefix">读取权限名称，也是新增、编辑和删除权限的前缀。</param>
    public LimeMetaAuthorizeAttribute(string permissionPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionPrefix);
        if (permissionPrefix.Contains('|', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "权限前缀不能包含备选权限分隔符“|”。",
                nameof(permissionPrefix));
        }

        PermissionPrefix = permissionPrefix;
        Read = permissionPrefix;
        Create = $"{permissionPrefix}.新增";
        Update = $"{permissionPrefix}.编辑";
        Delete = $"{permissionPrefix}.删除";
    }

    /// <summary>
    /// 权限前缀。
    /// </summary>
    public string PermissionPrefix { get; }

    /// <summary>
    /// 查询和聚合所需权限。
    /// 可用 <c>|</c> 分隔多个备选权限，满足其一即可。
    /// </summary>
    public string Read { get; set; }

    /// <summary>
    /// 新增所需权限。
    /// 可用 <c>|</c> 分隔多个备选权限，满足其一即可。
    /// </summary>
    public string Create { get; set; }

    /// <summary>
    /// 修改所需权限。
    /// 可用 <c>|</c> 分隔多个备选权限，满足其一即可。
    /// </summary>
    public string Update { get; set; }

    /// <summary>
    /// 删除所需权限。
    /// 可用 <c>|</c> 分隔多个备选权限，满足其一即可。
    /// </summary>
    public string Delete { get; set; }
}
