using System.Net;
using System.Net.Mail;
using LimeMeta.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimeMeta.Mail;

/// <summary>
/// 基于 SMTP 的默认邮件发送实现。
/// </summary>
public sealed class SmtpLimeMetaEmailSender(
    IOptions<LimeMetaConfiguration> options,
    IHostEnvironment environment,
    ILogger<SmtpLimeMetaEmailSender> logger) : ILimeMetaEmailSender
{
    private readonly SmtpConfiguration _smtp = options.Value.Smtp;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        if (!_smtp.IsConfigured)
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning(
                    "SMTP 未完整配置，开发环境跳过真实发信。To={To}, Subject={Subject}, Body={Body}",
                    to,
                    subject,
                    body);
                return;
            }

            throw new InvalidOperationException("邮件服务未配置，请填写 LimeMeta:Smtp。");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.From, _smtp.FromDisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(to);

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.UseSsl,
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password)
        };

        ct.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, ct);
    }
}
