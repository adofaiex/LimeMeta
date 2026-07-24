namespace LimeMeta.Security;

/// <summary>
/// LimeMeta 密码哈希服务。业务项目可以在 DI 中替换默认实现。
/// </summary>
public interface ILimeMetaPasswordHasher
{
    /// <summary>
    /// 为明文密码生成包含独立随机盐的完整哈希。
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// 验证明文密码与已保存哈希是否匹配。
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
