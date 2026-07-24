using FreeSql;
using LimeMeta.Configurations;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LimeMeta.Tests;

public sealed class ConfigurationValidatorTests
{
    [Fact]
    public void Production_RejectsWeakOrExampleSecrets()
    {
        var validator = new LimeMetaConfigurationValidator(new TestEnvironment(Environments.Production));
        var options = ValidOptions();
        options.JwtSignKey = "too-short";
        options.AdminUserPassword = "change-me-admin-password";

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, item => item.Contains("32"));
        Assert.Contains(result.Failures, item => item.Contains("示例管理员密码"));
    }

    [Fact]
    public void Development_AllowsDocumentedExamplePassword()
    {
        var validator = new LimeMetaConfigurationValidator(new TestEnvironment(Environments.Development));
        var options = ValidOptions();
        options.AdminUserPassword = "change-me-admin-password";

        Assert.True(validator.Validate(null, options).Succeeded);
    }

    [Fact]
    public void MissingConnectionString_IsAlwaysRejected()
    {
        var validator = new LimeMetaConfigurationValidator(new TestEnvironment(Environments.Development));
        var options = ValidOptions();
        options.ConnectionString = "";

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, item => item.Contains("ConnectionString"));
    }

    [Fact]
    public void Production_RejectsDocumentedExampleConnectionAndJwtValues()
    {
        var validator = new LimeMetaConfigurationValidator(new TestEnvironment(Environments.Production));
        var options = ValidOptions();
        options.ConnectionString =
            "Server=127.0.0.1;Database=test;Uid=test;Pwd=change-me;";
        options.JwtSignKey =
            "development-only-jwt-key-change-before-production-1234567890";

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, item => item.Contains("示例数据库连接串"));
        Assert.Contains(result.Failures, item => item.Contains("示例 JWT"));
    }

    private static LimeMetaConfiguration ValidOptions() => new()
    {
        ConnectionString = "Server=127.0.0.1;Database=test;Uid=test;Pwd=test;",
        DataType = DataType.MySql,
        AdminUserName = "admin",
        AdminUserPassword = "a-strong-admin-password",
        JwtSignKey = "0123456789abcdef0123456789abcdef"
    };

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "LimeMeta.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
