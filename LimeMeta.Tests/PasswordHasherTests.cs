using LimeMeta.Security;
using LimeMeta.Data;
using LimeMeta.Logics;
using Moq;

namespace LimeMeta.Tests;

public sealed class PasswordHasherTests
{
    private readonly ILimeMetaPasswordHasher _hasher = new BcryptLimeMetaPasswordHasher();

    [Fact]
    public void HashPassword_UsesAnIndependentSalt()
    {
        var first = _hasher.HashPassword("correct horse battery staple");
        var second = _hasher.HashPassword("correct horse battery staple");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.VerifyPassword("correct horse battery staple", first));
        Assert.True(_hasher.VerifyPassword("correct horse battery staple", second));
    }

    [Fact]
    public void VerifyPassword_RejectsWrongOrMalformedValues()
    {
        var hash = _hasher.HashPassword("expected-password");

        Assert.False(_hasher.VerifyPassword("wrong-password", hash));
        Assert.False(_hasher.VerifyPassword("expected-password", "not-a-bcrypt-hash"));
        Assert.False(_hasher.VerifyPassword("", hash));
    }

    [Fact]
    public void Login_WhenBeforeLoginCancels_DoesNotAccessUserStore()
    {
        var meta = new Mock<ILimeMeta>(MockBehavior.Strict);
        var passwordHasher = new Mock<ILimeMetaPasswordHasher>(MockBehavior.Strict);
        EventHandler<UserLogic.BeforeLoginEventArgs> handler = (_, args) =>
            args.Cancel = true;

        UserLogic.BeforeLogin += handler;
        try
        {
            var result = UserLogic.Login(
                meta.Object,
                passwordHasher.Object,
                "blocked-user",
                "not-checked");

            Assert.Null(result.Name);
            Assert.Null(result.Token);
            meta.VerifyNoOtherCalls();
            passwordHasher.VerifyNoOtherCalls();
        }
        finally
        {
            UserLogic.BeforeLogin -= handler;
        }
    }
}
