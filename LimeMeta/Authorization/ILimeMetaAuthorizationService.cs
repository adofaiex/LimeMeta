using LimeMeta.Data;

namespace LimeMeta.Authorization;

/// <summary>
/// 对自动生成的模型查询和修改进行授权。
/// </summary>
public interface ILimeMetaAuthorizationService
{
    /// <summary>
    /// 确认用户有权执行指定模型操作；未授权时应抛出 <see cref="UnauthorizedAccessException"/>。
    /// </summary>
    void EnsureAuthorized(ILimeMeta meta, Guid userId, Type modelType, LimeMetaOperation operation);
}

/// <summary>
/// LimeMeta 自动模型操作。
/// </summary>
public enum LimeMetaOperation
{
    Query,
    Aggregate,
    Insert,
    Update,
    Delete
}
