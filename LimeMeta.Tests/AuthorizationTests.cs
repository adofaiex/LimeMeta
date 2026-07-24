using System.Linq.Expressions;
using FreeSql;
using LimeMeta.Authorization;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LimeMeta.Tests;

public sealed class AuthorizationTests
{
    [Fact]
    public void DefaultPolicy_AllowsBusinessModelsButRejectsSystemMutationForNonAdmin()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new LimeMetaConfiguration { AdminUserName = "admin" });
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

        var policy = new DefaultLimeMetaAuthorizationService();
        var userId = Guid.NewGuid();

        policy.EnsureAuthorized(meta.Object, userId, typeof(BusinessModel), LimeMetaOperation.Insert);
        policy.EnsureAuthorized(meta.Object, userId, typeof(User), LimeMetaOperation.Query);
        Assert.Throws<UnauthorizedAccessException>(() =>
            policy.EnsureAuthorized(meta.Object, userId, typeof(User), LimeMetaOperation.Update));
    }

    private sealed class BusinessModel : BaseObject;
}
