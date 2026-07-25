using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimeMeta.Mail;

/// <summary>
/// ????????
/// </summary>
public sealed class EmailVerificationService
{
    public const string PurposeRegister = "register";

    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ILimeMeta _meta;
    private readonly ILimeMetaEmailSender _emailSender;
    private readonly IOptions<LimeMetaConfiguration> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        ILimeMeta meta,
        ILimeMetaEmailSender emailSender,
        IOptions<LimeMetaConfiguration> options,
        IHostEnvironment environment,
        ILogger<EmailVerificationService> logger)
    {
        _meta = meta;
        _emailSender = emailSender;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public static bool IsValidEmail(string email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

    public static string NormalizeEmail(string email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    public async Task SendRegisterCodeAsync(string email, CancellationToken ct = default)
    {
        email = NormalizeEmail(email);
        if (!IsValidEmail(email))
        {
            throw new LimeMetaException("????????");
        }

        if (_meta.Query<User>().Any(x => x.Email == email))
        {
            throw new LimeMetaException("????????");
        }

        var latest = _meta.Query<EmailVerificationCode>()
            .Where(x => x.Email == email && x.Purpose == PurposeRegister && !x.Used)
            .OrderByDescending(x => x.Created)
            .First();

        if (latest is not null)
        {
            var createdAt = FromReadableLong(latest.Created);
            if (DateTime.UtcNow - createdAt < ResendInterval)
            {
                throw new LimeMetaException("?????????????");
            }
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var record = new EmailVerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            CodeHash = HashCode(code),
            Purpose = PurposeRegister,
            ExpireAt = DateTime.UtcNow.Add(CodeTtl),
            Used = false
        };
        _meta.Insert([record], null, enableLogic: false);

        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("???????? Email={Email} Code={Code}", email, code);
        }

        if (!_options.Value.Smtp.IsConfigured && !_environment.IsDevelopment())
        {
            throw new LimeMetaException("????????");
        }

        await _emailSender.SendAsync(
            email,
            "?????",
            $"???????? {code}?{CodeTtl.TotalMinutes:0} ????????????????",
            ct);
    }

    public void ConsumeRegisterCode(string email, string code)
    {
        email = NormalizeEmail(email);
        code = (code ?? string.Empty).Trim();
        if (!IsValidEmail(email))
        {
            throw new LimeMetaException("????????");
        }

        if (code.Length != 6 || code.Any(c => c is < '0' or > '9'))
        {
            throw new LimeMetaException("???????");
        }

        var now = DateTime.UtcNow;
        var records = _meta.Query<EmailVerificationCode>()
            .Where(x =>
                x.Email == email
                && x.Purpose == PurposeRegister
                && !x.Used
                && x.ExpireAt >= now)
            .OrderByDescending(x => x.Created)
            .ToList();

        var matched = records.FirstOrDefault(x => FixedTimeEquals(x.CodeHash, HashCode(code)));
        if (matched is null)
        {
            throw new LimeMetaException("???????????");
        }

        matched.Used = true;
        _meta.Update([matched], [nameof(EmailVerificationCode.Used)], null, enableLogic: false);

        foreach (var other in records.Where(x => x.Id != matched.Id))
        {
            other.Used = true;
            _meta.Update([other], [nameof(EmailVerificationCode.Used)], null, enableLogic: false);
        }
    }

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static bool FixedTimeEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static DateTime FromReadableLong(long value)
    {
        var text = value.ToString();
        if (text.Length < 17
            || !int.TryParse(text[..4], out var year)
            || !int.TryParse(text[4..6], out var month)
            || !int.TryParse(text[6..8], out var day)
            || !int.TryParse(text[8..10], out var hour)
            || !int.TryParse(text[10..12], out var minute)
            || !int.TryParse(text[12..14], out var second)
            || !int.TryParse(text[14..17], out var ms))
        {
            return DateTime.MinValue;
        }

        try
        {
            return new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Local)
                .ToUniversalTime();
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
