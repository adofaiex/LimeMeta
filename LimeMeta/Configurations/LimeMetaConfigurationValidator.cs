using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LimeMeta.Configurations;

internal sealed class LimeMetaConfigurationValidator(IHostEnvironment environment)
    : IValidateOptions<LimeMetaConfiguration>
{
    private static readonly string[] ExamplePasswords =
    [
        "change-me-admin-password",
        "change-me",
        "admin",
        "password"
    ];
    private static readonly string[] ExampleMarkers =
    [
        "change-me",
        "replace-with",
        "development-only",
        "Password=postgres",
        "Pwd=postgres"
    ];

    public ValidateOptionsResult Validate(string? name, LimeMetaConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add("LimeMeta:ConnectionString 不能为空。");
        }
        else if (!environment.IsDevelopment() && ContainsExampleMarker(options.ConnectionString))
        {
            failures.Add("生产环境禁止使用示例数据库连接串。");
        }

        if (Encoding.UTF8.GetByteCount(options.JwtSignKey ?? string.Empty) < 32)
        {
            failures.Add("LimeMeta:JwtSignKey 至少需要 32 个 UTF-8 字节。");
        }
        else if (!environment.IsDevelopment() &&
                 ContainsExampleMarker(options.JwtSignKey ?? string.Empty))
        {
            failures.Add("生产环境禁止使用示例 JWT 密钥。");
        }

        if (string.IsNullOrWhiteSpace(options.AdminUserName))
        {
            failures.Add("LimeMeta:AdminUserName 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(options.AdminUserPassword))
        {
            failures.Add("LimeMeta:AdminUserPassword 不能为空。");
        }
        else if (!environment.IsDevelopment() &&
                 ExamplePasswords.Contains(options.AdminUserPassword, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add("生产环境禁止使用示例管理员密码。");
        }
        else if (!environment.IsDevelopment() && options.AdminUserPassword.Length < 12)
        {
            failures.Add("生产环境管理员密码至少需要 12 个字符。");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool ContainsExampleMarker(string value) =>
        ExampleMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
