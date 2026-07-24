using System.Text.Json;

namespace LimeMeta.Tests;

public sealed class TemplateContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Template_DefaultsToMatchingStableVersionAndMySql()
    {
        var templateRoot = Path.Combine(RepositoryRoot, "templates", "LimeMeta.Service");
        var templateJson = File.ReadAllText(
            Path.Combine(templateRoot, ".template.config", "template.json"));
        using var document = JsonDocument.Parse(templateJson);
        var defaultVersion = document.RootElement
            .GetProperty("symbols")
            .GetProperty("limeMetaVersion")
            .GetProperty("defaultValue")
            .GetString();
        var developmentConfig = File.ReadAllText(
            Path.Combine(templateRoot, "LimeMetaService.WebAPI", "appsettings.Development.yml"));

        Assert.Equal("1.0.0", defaultVersion);
        Assert.Contains("DataType: \"MySql\"", developmentConfig);
        Assert.Contains("Port=3306", developmentConfig);
        Assert.Contains("Path: \"./FileStore\"", developmentConfig);
        Assert.DoesNotContain("Path: \"/FileStore\"", developmentConfig);
    }

    [Fact]
    public void Template_DoesNotContainPrivateFeedOrUnusedSystemSeed()
    {
        var templateRoot = Path.Combine(RepositoryRoot, "templates", "LimeMeta.Service");

        Assert.False(File.Exists(Path.Combine(templateRoot, "NuGet.config")));
        Assert.False(File.Exists(Path.Combine(
            templateRoot,
            "LimeMetaService.WebAPI",
            "Seed",
            "system.yml")));
        Assert.DoesNotContain(
            "nuget.pkg.github.com",
            string.Join('\n', Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Select(File.ReadAllText)));
    }

    [Fact]
    public void Template_ContainsCompleteChineseDeveloperDocumentation()
    {
        var templateRoot = Path.Combine(RepositoryRoot, "templates", "LimeMeta.Service");
        var expectedDocuments = new[]
        {
            "README.md",
            Path.Combine("docs", "01-overview.md"),
            Path.Combine("docs", "02-models-and-graphql.md"),
            Path.Combine("docs", "03-users-and-authorization.md"),
            Path.Combine("docs", "04-logic-and-extensions.md"),
            Path.Combine("docs", "05-configuration-and-deployment.md")
        };

        foreach (var relativePath in expectedDocuments)
        {
            Assert.True(
                File.Exists(Path.Combine(templateRoot, relativePath)),
                $"模板缺少开发文档：{relativePath}");
        }

        var allDocumentation = string.Join(
            '\n',
            expectedDocuments.Select(relativePath =>
                File.ReadAllText(Path.Combine(templateRoot, relativePath))));

        Assert.Contains("ILimeMetaAuthorizationService", allDocumentation);
        Assert.Contains("createUser", allDocumentation);
        Assert.Contains("BaseParentChildren", allDocumentation);
        Assert.Contains("FastEndpoints", allDocumentation);
        Assert.Contains("WebSocket", allDocumentation);
        Assert.Contains("Seed/Perm.yaml", allDocumentation);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "LimeMeta.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new DirectoryNotFoundException("无法定位 LimeMeta 仓库根目录。");
    }
}
