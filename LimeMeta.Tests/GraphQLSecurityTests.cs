using FreeSql;
using FreeSql.DataAnnotations;
using HotChocolate.Execution;
using LimeMeta.Attributes;
using LimeMeta.Configurations;
using LimeMeta.GraphQL;
using LimeMeta.Logics;
using LimeMeta.Models;
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

    [Fact]
    public async Task Schema_DoesNotGenerateOperationsForDisabledModel()
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
        services.AddLimeMetaModule(typeof(GraphQLSecurityTests).Assembly);
        services.AddLimeMetaGraphQL();
        await using var provider = services.BuildServiceProvider();

        var executor = await provider
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();
        var schema = executor.Schema.ToString();

        Assert.Contains(
            nameof(GraphQLVisibleTestModel),
            schema,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            nameof(GraphQLDisabledTestModel),
            schema,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "LimeMeta.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

[Table(Name = "graphql_visible_test_model")]
[LimeMetaAllowAuthenticated(Read = true, Create = true, Update = true, Delete = true)]
public sealed class GraphQLVisibleTestModel : BaseObject
{
}

public sealed class GraphQLVisibleTestModelDto : BaseDto
{
}

[Table(Name = "graphql_disabled_test_model")]
[DisableGraphQL]
public sealed class GraphQLDisabledTestModel : BaseObject
{
}

public sealed class GraphQLDisabledTestModelDto : BaseDto
{
}
