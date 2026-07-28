using System.Linq.Expressions;
using FreeSql;
using LimeMeta.Attributes;
using LimeMeta.Authorization;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LimeMeta.Tests;

public sealed class AuthorizationTests
{
    [Theory]
    [InlineData(LimeMetaOperation.Query, "工具")]
    [InlineData(LimeMetaOperation.Aggregate, "工具")]
    [InlineData(LimeMetaOperation.Insert, "工具.上传")]
    [InlineData(LimeMetaOperation.Update, "工具.编辑")]
    [InlineData(LimeMetaOperation.Delete, "工具.删除")]
    public void ModelPolicy_MapsOperationsToDeclaredPermissions(
        LimeMetaOperation operation,
        string expectedPermission)
    {
        var requirement = ModelAuthorizationPolicy.Resolve(
            typeof(SecuredBusinessModel),
            operation);

        Assert.Equal(ModelAuthorizationRequirementKind.Permission, requirement.Kind);
        Assert.Equal(expectedPermission, requirement.Permission);
    }

    [Fact]
    public void ModelPolicy_RejectsBusinessModelWithoutAccessDeclaration()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ModelAuthorizationPolicy.Validate(typeof(UnconfiguredBusinessModel)));

        Assert.Contains("LimeMetaAuthorize", exception.Message);
        Assert.Contains("LimeMetaAllowAuthenticated", exception.Message);
        Assert.Contains("DisableGraphQL", exception.Message);
    }

    [Fact]
    public void ModelPolicy_RejectsConflictingAccessDeclarations()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ModelAuthorizationPolicy.Validate(typeof(ConflictingBusinessModel)));

        Assert.Contains("只能声明一种", exception.Message);
    }

    [Fact]
    public void ModelPolicy_RejectsEmptyAuthenticatedAccessDeclaration()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ModelAuthorizationPolicy.Validate(typeof(EmptyAuthenticatedBusinessModel)));

        Assert.Contains("至少需要允许一种操作", exception.Message);
    }

    [Theory]
    [InlineData("工具", true)]
    [InlineData("审核管理.工具审核", true)]
    [InlineData("工具.编辑", false)]
    public void ModelPolicy_AcceptsAnyOneOfDeclaredPermissions(
        string grantedPermission,
        bool expected)
    {
        var requirement = ModelAuthorizationPolicy.Resolve(
            typeof(MultipleReadPermissionsModel),
            LimeMetaOperation.Query);
        var permissions = new HashSet<string>(
            [grantedPermission],
            StringComparer.Ordinal);

        Assert.Equal(
            expected,
            ModelAuthorizationPolicy.HasAnyPermission(
                permissions,
                requirement.Permission!));
    }

    [Fact]
    public void ModelPolicy_RejectsEmptyPermissionAlternative()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ModelAuthorizationPolicy.Validate(typeof(EmptyPermissionAlternativeModel)));

        Assert.Contains("包含空的备选权限", exception.Message);
    }

    [Fact]
    public void AuthorizeAttribute_RejectsAlternativeDelimiterInPermissionPrefix()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new LimeMetaAuthorizeAttribute("工具|审核管理"));

        Assert.Contains("权限前缀不能包含", exception.Message);
    }

    [Fact]
    public void DefaultPolicy_AllowsExplicitReadButRejectsUnapprovedWriteForNonAdmin()
    {
        var services = new ServiceCollection();
        var configuration = new LimeMetaConfiguration
        {
            AdminUserName = "admin",
            AdminPerm = "admin"
        };
        services.AddSingleton(configuration);
        using var provider = services.BuildServiceProvider();

        var users = new Mock<ISelect<User>>();
        users
            .Setup(select => select.Where(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(users.Object);
        users.Setup(select => select.First()).Returns((User)null!);

        var meta = new Mock<ILimeMeta>();
        meta.SetupGet(item => item.ScopeFactory)
            .Returns(provider.GetRequiredService<IServiceScopeFactory>());
        meta.Setup(item => item.Query<User>()).Returns(users.Object);

        var policy = new DefaultLimeMetaAuthorizationService(configuration);
        var userId = Guid.NewGuid();

        policy.EnsureAuthorized(
            meta.Object,
            userId,
            typeof(AuthenticatedReadModel),
            LimeMetaOperation.Query);
        policy.EnsureAuthorized(meta.Object, userId, typeof(User), LimeMetaOperation.Query);
        Assert.Throws<UnauthorizedAccessException>(() =>
            policy.EnsureAuthorized(
                meta.Object,
                userId,
                typeof(AuthenticatedReadModel),
                LimeMetaOperation.Insert));
        Assert.Throws<UnauthorizedAccessException>(() =>
            policy.EnsureAuthorized(meta.Object, userId, typeof(User), LimeMetaOperation.Update));
    }

    [LimeMetaAuthorize("工具", Create = "工具.上传")]
    private sealed class SecuredBusinessModel : BaseObject;

    [LimeMetaAllowAuthenticated(Read = true)]
    private sealed class AuthenticatedReadModel : BaseObject;

    [LimeMetaAuthorize("冲突")]
    [DisableGraphQL]
    private sealed class ConflictingBusinessModel : BaseObject;

    [LimeMetaAllowAuthenticated]
    private sealed class EmptyAuthenticatedBusinessModel : BaseObject;

    [LimeMetaAuthorize(
        "工具",
        Read = "工具 | 审核管理.工具审核")]
    private sealed class MultipleReadPermissionsModel : BaseObject;

    [LimeMetaAuthorize(
        "工具",
        Read = "工具||审核管理.工具审核")]
    private sealed class EmptyPermissionAlternativeModel : BaseObject;

    private sealed class UnconfiguredBusinessModel : BaseObject;
}
