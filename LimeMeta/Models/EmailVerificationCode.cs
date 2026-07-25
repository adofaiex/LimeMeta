using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Models;

/// <summary>
/// 邮箱验证码。
/// </summary>
[Table(Name = "email_verification_code")]
[LimeMetaIgnoreGraphQL]
public sealed class EmailVerificationCode : BaseAudit
{
    [Column(Name = "email", StringLength = 200), Indexed]
    public required string Email { get; set; }

    [Column(Name = "code_hash", StringLength = 128)]
    public required string CodeHash { get; set; }

    [Column(Name = "purpose", StringLength = 32), Indexed]
    public required string Purpose { get; set; }

    [Column(Name = "expire_at"), Indexed]
    public DateTime ExpireAt { get; set; }

    [Column(Name = "used"), Indexed]
    public bool Used { get; set; }
}

/// <summary>
/// EmailVerificationCodeDto
/// </summary>
public sealed class EmailVerificationCodeDto : BaseDto
{
    public required string Email { get; set; }

    public required string CodeHash { get; set; }

    public required string Purpose { get; set; }

    public DateTime ExpireAt { get; set; }

    public bool Used { get; set; }
}
