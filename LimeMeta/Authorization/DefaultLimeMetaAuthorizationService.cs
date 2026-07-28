using System.Collections.Concurrent;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Logics;

namespace LimeMeta.Authorization;

internal sealed class DefaultLimeMetaAuthorizationService(
    LimeMetaConfiguration configuration) : ILimeMetaAuthorizationService
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _permissionCache = [];

    public void EnsureAuthorized(ILimeMeta meta, Guid userId, Type modelType, LimeMetaOperation operation)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ArgumentNullException.ThrowIfNull(modelType);

        var requirement = ModelAuthorizationPolicy.Resolve(modelType, operation);
        if (requirement.Kind == ModelAuthorizationRequirementKind.Authenticated)
        {
            return;
        }

        var permissions = _permissionCache.GetOrAdd(
            userId,
            id => new HashSet<string>(
                UserLogic.GetPerms(meta, id).Select(item => item.Name),
                StringComparer.Ordinal));

        if (permissions.Contains(configuration.AdminPerm))
        {
            return;
        }

        if (requirement.Kind == ModelAuthorizationRequirementKind.Denied)
        {
            throw new UnauthorizedAccessException(
                $"模型 {modelType.Name} 不允许执行 {operation} 操作。");
        }

        if (requirement.Permission is null ||
            !ModelAuthorizationPolicy.HasAnyPermission(
                permissions,
                requirement.Permission))
        {
            throw new UnauthorizedAccessException(
                $"缺少权限：{requirement.Permission}");
        }
    }
}
