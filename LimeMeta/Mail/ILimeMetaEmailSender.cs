namespace LimeMeta.Mail;

/// <summary>
/// LimeMeta 邮件发送抽象。
/// </summary>
public interface ILimeMetaEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
