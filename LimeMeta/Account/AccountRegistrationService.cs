using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Files;
using LimeMeta.Logics;
using LimeMeta.Mail;
using LimeMeta.Models;
using LimeMeta.Security;
using Microsoft.Extensions.Options;
using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Account;

/// <summary>
/// ????????????
/// </summary>
public sealed class AccountRegistrationService(
    ILimeMeta meta,
    ILimeMetaPasswordHasher passwordHasher,
    EmailVerificationService emailVerification,
    FileUrlResolver fileUrlResolver,
    IOptions<LimeMetaConfiguration> options)
{
    public const int MinPasswordLength = 8;

    public Task SendRegisterCodeAsync(string email, CancellationToken ct = default)
    {
        EnsureSelfRegisterEnabled();
        return emailVerification.SendRegisterCodeAsync(email, ct);
    }

    public async Task<LoginResult> RegisterAsync(
        string username,
        string password,
        string email,
        string? code,
        CancellationToken ct = default)
    {
        EnsureSelfRegisterEnabled();

        username = (username ?? string.Empty).Trim();
        email = EmailVerificationService.NormalizeEmail(email);
        password ??= string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new LimeMetaException("????????");
        }

        if (password.Length < MinPasswordLength)
        {
            throw new LimeMetaException($"?????? {MinPasswordLength} ??");
        }

        if (!EmailVerificationService.IsValidEmail(email))
        {
            throw new LimeMetaException("????????");
        }

        if (meta.Query<User>().Any(x => x.Username == username))
        {
            throw new LimeMetaException("????????");
        }

        if (meta.Query<User>().Any(x => x.Email == email))
        {
            throw new LimeMetaException("????????");
        }

        if (options.Value.RegisterRequireEmailCode)
        {
            emailVerification.ConsumeRegisterCode(email, code ?? string.Empty);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = username,
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.HashPassword(password)
        };
        meta.Insert([user], user.Id);

        return await BuildLoginResultAsync(user, password, ct);
    }

    public async Task<LoginResult> EnrichLoginAsync(LoginResult login, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login.Token))
        {
            return login;
        }

        var user = meta.Query<User>().FirstOrDefault(x => x.Username == username);
        if (user is null)
        {
            return login;
        }

        login.UserId = user.Id;
        login.Name ??= user.Name;
        login.Email = user.Email;
        login.AvatarUrl = await fileUrlResolver.ResolveAsync(user.AvatarFileId, persist: true, ct);
        return login;
    }

    public async Task<LoginResult> UpdateMyAvatarAsync(Guid userId, Guid fileId, CancellationToken ct = default)
    {
        _ = meta.Query<FileInfo>().FirstOrDefault(x => x.Id == fileId)
            ?? throw new LimeMetaException("????????");

        var user = meta.Query<User>().FirstOrDefault(x => x.Id == userId)
            ?? throw new LimeMetaException("??????");

        user.AvatarFileId = fileId;
        meta.Update([user], [nameof(User.AvatarFileId)], userId);

        return new LoginResult
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = await fileUrlResolver.ResolveAsync(fileId, persist: true, ct)
        };
    }

    private async Task<LoginResult> BuildLoginResultAsync(User user, string password, CancellationToken ct)
    {
        var login = UserLogic.Login(meta, passwordHasher, user.Username, password);
        if (string.IsNullOrWhiteSpace(login.Token))
        {
            throw new LimeMetaException("??????????????????");
        }

        return await EnrichLoginAsync(login, user.Username, ct);
    }

    private void EnsureSelfRegisterEnabled()
    {
        if (!options.Value.AllowSelfRegister)
        {
            throw new LimeMetaException("??????????");
        }
    }
}
