using System.Text.Json;
using System.Xml.Linq;

namespace LimeMeta.Tests;

public sealed class TemplateContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Template_EmbedsFrameworkSourceAndUsesProjectReferences()
    {
        var templateRoot = Path.Combine(RepositoryRoot, "templates", "LimeMeta.Service");
        var templateJsonPath = Path.Combine(templateRoot, ".template.config", "template.json");
        using var document = JsonDocument.Parse(File.ReadAllText(templateJsonPath));
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("symbols", out _));
        Assert.DoesNotContain("__LIMEMETA_VERSION__", File.ReadAllText(templateJsonPath));

        var businessProject = File.ReadAllText(
            Path.Combine(templateRoot, "LimeMetaService", "LimeMetaService.csproj"));
        Assert.Contains(@"ProjectReference Include=""..\LimeMeta\LimeMeta.csproj""", businessProject);
        Assert.Contains(@"ProjectReference Include=""..\LimeMeta.GraphQL\LimeMeta.GraphQL.csproj""", businessProject);
        Assert.DoesNotContain(@"PackageReference Include=""LimeMeta", businessProject);

        var solution = File.ReadAllText(Path.Combine(templateRoot, "LimeMetaService.sln"));
        Assert.Equal(4, solution.Split('\n').Count(line => line.StartsWith("Project(", StringComparison.Ordinal)));
        Assert.Contains(@"""LimeMeta"", ""LimeMeta\LimeMeta.csproj""", solution);
        Assert.Contains(@"""LimeMeta.GraphQL"", ""LimeMeta.GraphQL\LimeMeta.GraphQL.csproj""", solution);

        var templateProject = XDocument.Load(Path.Combine(RepositoryRoot, "LimeMeta.Templates.csproj"));
        var packedIncludes = templateProject
            .Descendants("Content")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(value => value is not null)
            .ToArray();
        Assert.Contains("LimeMeta/**/*.cs", packedIncludes);
        Assert.Contains("LimeMeta.GraphQL/**/*.cs", packedIncludes);
        Assert.Contains("LICENSE", packedIncludes);
        Assert.Contains("NOTICE", packedIncludes);
    }

    [Fact]
    public void Template_DefaultsToMySqlAndContainsNoFrameworkPackageReferences()
    {
        var templateRoot = Path.Combine(RepositoryRoot, "templates", "LimeMeta.Service");
        var developmentConfig = File.ReadAllText(
            Path.Combine(templateRoot, "LimeMetaService.WebAPI", "appsettings.Development.yml"));
        var templateText = string.Join(
            '\n',
            Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Select(File.ReadAllText));

        Assert.Contains("DataType: \"MySql\"", developmentConfig);
        Assert.Contains("Port=3306", developmentConfig);
        Assert.Contains("Path: \"./FileStore\"", developmentConfig);
        Assert.DoesNotContain("Path: \"/FileStore\"", developmentConfig);
        Assert.DoesNotContain(@"PackageReference Include=""LimeMeta""", templateText);
        Assert.DoesNotContain(@"PackageReference Include=""LimeMeta.GraphQL""", templateText);
        Assert.DoesNotContain("__LIMEMETA_VERSION__", templateText);
    }

    [Fact]
    public void Template_DocumentsPublicNugetFeedWithoutEmbeddedCredentials()
    {
        var templateRoot = Path.Combine(RepositoryRoot, "templates", "LimeMeta.Service");

        Assert.False(File.Exists(Path.Combine(templateRoot, "NuGet.config")));
        Assert.False(File.Exists(Path.Combine(
            templateRoot,
            "LimeMetaService.WebAPI",
            "Seed",
            "system.yml")));
        var templateText = string.Join(
            '\n',
            Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Select(File.ReadAllText));

        Assert.Contains("https://api.nuget.org/v3/index.json", templateText);
        Assert.DoesNotContain("nuget.pkg.github.com/adofaiex/index.json", templateText);
        Assert.DoesNotContain("ClearTextPassword", templateText);
        Assert.DoesNotContain("memsys-lizi", templateText);
        Assert.DoesNotMatch(@"gh[pousr]_[A-Za-z0-9_]{20,}", templateText);
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
        Assert.Contains("ProjectReference", allDocumentation);
        Assert.Contains("框架源码", allDocumentation);
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
