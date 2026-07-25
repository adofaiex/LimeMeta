using FreeSql;
using HotChocolate.Execution;
using LimeMeta.Configurations;
using LimeMeta.GraphQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LimeMeta.Tests;

public sealed class GraphQLSecurityTests
{
    [Fact]
    public async Task Schema_DoesNotExposePasswordHashOrLegacyCryptMutation()
    {
        var settings = new Dictionary<string, string?>
        {
            ["LimeMeta:ConnectionString"] = "Server=127.0.0.1;Database=test;Uid=test;Pwd=test;",
            ["LimeMeta:DataType"] = DataType.MySql.ToString(),
            ["LimeMeta:AdminUserName"] = "admin",
            ["LimeMeta:AdminUserPassword"] = "local-test-password",
            ["LimeMeta:JwtSignKey"] = "0123456789abcdef0123456789abcdef"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLimeMeta(configuration, new TestEnvironment());
        services.AddLimeMetaGraphQL();
        await using var provider = services.BuildServiceProvider();

        var executor = await provider
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();
        var schema = executor.Schema.ToString();

        Assert.DoesNotContain("passwordHash", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("crypt(", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("login(", schema, StringComparison.Ordinal);
        Assert.Contains("createUser(", schema, StringComparison.Ordinal);
        Assert.Contains("changePassword(", schema, StringComparison.Ordinal);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "LimeMeta.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
