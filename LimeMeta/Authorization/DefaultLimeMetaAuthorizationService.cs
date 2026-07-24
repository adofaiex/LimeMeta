using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;

namespace LimeMeta.Authorization;

internal sealed class DefaultLimeMetaAuthorizationService : ILimeMetaAuthorizationService
{
    public void EnsureAuthorized(ILimeMeta meta, Guid userId, Type modelType, LimeMetaOperation operation)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ArgumentNullException.ThrowIfNull(modelType);

        if (operation is LimeMetaOperation.Query or LimeMetaOperation.Aggregate)
        {
            return;
        }

        var isSystemModel = modelType.Assembly == typeof(User).Assembly;
        if (isSystemModel && !UserLogic.IsAdmin(meta, userId))
        {
            throw new UnauthorizedAccessException($"只有管理员可以修改系统模型 {modelType.Name}。");
        }
    }
}
