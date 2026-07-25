using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using LimeMeta.Account;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;
using LimeMeta.Security;

namespace LimeMeta.GraphQL;

internal sealed class Mutation
{
    [AllowAnonymous]
    public async Task<LoginResult> Login(
        [Service] ILimeMeta meta,
        [Service] ILimeMetaPasswordHasher passwordHasher,
        [Service] AccountRegistrationService accountService,
        IResolverContext context,
        string username,
        string password,
        CancellationToken ct)
    {
        var result = UserLogic.Login(meta, passwordHasher, username, password, context);
        return await accountService.EnrichLoginAsync(result, username, ct);
    }

    [AllowAnonymous]
    public async Task<bool> SendRegisterCode(
        string email,
        [Service] AccountRegistrationService accountService,
        CancellationToken ct)
    {
        try
        {
            await accountService.SendRegisterCodeAsync(email, ct);
            return true;
        }
        catch (LimeMetaException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    [AllowAnonymous]
    public async Task<LoginResult> Register(
        string username,
        string password,
        string email,
        string? code,
        [Service] AccountRegistrationService accountService,
        CancellationToken ct)
    {
        try
        {
            return await accountService.RegisterAsync(username, password, email, code, ct);
        }
        catch (LimeMetaException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    public async Task<LoginResult> UpdateMyAvatar(
        Guid fileId,
        [Service] AccountRegistrationService accountService,
        IResolverContext context,
        CancellationToken ct)
    {
        try
        {
            return await accountService.UpdateMyAvatarAsync(GetCurrentUserId(context), fileId, ct);
        }
        catch (LimeMetaException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    public Guid CreateUser(
        [Service] ILimeMeta meta,
        [Service] ILimeMetaPasswordHasher passwordHasher,
        IResolverContext context,
        string name,
        string username,
        string password,
        string? phone,
        string? email,
        IReadOnlyList<Guid>? roleIds)
    {
        var authUserId = GetCurrentUserId(context);
        EnsureAdmin(meta, authUserId);

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim().ToLowerInvariant();
            if (meta.Query<User>().Any(x => x.Email == email))
            {
                throw new GraphQLException("该邮箱已被占用。");
            }
        }

        var user = new User
        {
            Name = name,
            Username = username,
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            PasswordHash = passwordHasher.HashPassword(password)
        };

        meta.Insert([user], authUserId, true, context);
        AssignRoles(meta, user.Id, roleIds, authUserId);
        return user.Id;
    }

    public bool UpdateUser(
        [Service] ILimeMeta meta,
        IResolverContext context,
        Guid userId,
        string? name,
        string? phone,
        string? email,
        IReadOnlyList<Guid>? roleIds)
    {
        var authUserId = GetCurrentUserId(context);
        EnsureAdmin(meta, authUserId);

        var user = meta.Query<User>().FirstOrDefault(item => item.Id == userId)
            ?? throw new GraphQLException($"用户 {userId} 不存在。");
        var fields = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
        {
            user.Name = name;
            fields.Add(nameof(User.Name));
        }

        if (phone is not null)
        {
            user.Phone = phone;
            fields.Add(nameof(User.Phone));
        }

        if (email is not null)
        {
            var normalized = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
            if (normalized is not null
                && meta.Query<User>().Any(x => x.Email == normalized && x.Id != userId))
            {
                throw new GraphQLException("该邮箱已被占用。");
            }

            user.Email = normalized;
            fields.Add(nameof(User.Email));
        }

        if (fields.Count > 0)
        {
            meta.Update([user], fields, authUserId, true, context);
        }

        if (roleIds is not null)
        {
            meta.Delete<UserRole>(item => item.UserId == userId, authUserId, true, context);
            AssignRoles(meta, userId, roleIds, authUserId);
        }

        return true;
    }

    public bool DeleteUser([Service] ILimeMeta meta, IResolverContext context, Guid userId)
    {
        var authUserId = GetCurrentUserId(context);
        EnsureAdmin(meta, authUserId);

        if (authUserId == userId)
        {
            throw new GraphQLException("管理员不能删除当前登录账号。");
        }

        return meta.Delete<User>(item => item.Id == userId, authUserId, true, context) > 0;
    }

    public bool ChangePassword(
        [Service] ILimeMeta meta,
        [Service] ILimeMetaPasswordHasher passwordHasher,
        IResolverContext context,
        string currentPassword,
        string newPassword)
    {
        return UserLogic.ChangePassword(
            meta,
            passwordHasher,
            GetCurrentUserId(context),
            currentPassword,
            newPassword);
    }

    public bool ResetUserPassword(
        [Service] ILimeMeta meta,
        [Service] ILimeMetaPasswordHasher passwordHasher,
        IResolverContext context,
        Guid userId,
        string newPassword)
    {
        return UserLogic.ResetPassword(
            meta,
            passwordHasher,
            GetCurrentUserId(context),
            userId,
            newPassword);
    }

    private static Guid GetCurrentUserId(IResolverContext context)
    {
        var claim = context.GetUser()?.Claims.FirstOrDefault(item => item.Type == UserLogic.ClaimUserId)
            ?? throw new GraphQLException("用户未认证。");
        return Guid.Parse(claim.Value);
    }

    private static void EnsureAdmin(ILimeMeta meta, Guid userId)
    {
        if (!UserLogic.IsAdmin(meta, userId))
        {
            throw new GraphQLException("只有管理员可以管理用户。");
        }
    }

    private static void AssignRoles(
        ILimeMeta meta,
        Guid userId,
        IReadOnlyList<Guid>? roleIds,
        Guid authUserId)
    {
        if (roleIds is null || roleIds.Count == 0)
        {
            return;
        }

        var distinctRoleIds = roleIds.Distinct().ToArray();
        var existingRoleIds = meta.Query<Role>()
            .Where(item => distinctRoleIds.Contains(item.Id))
            .ToList(item => item.Id);

        if (existingRoleIds.Count != distinctRoleIds.Length)
        {
            throw new GraphQLException("包含不存在的角色。");
        }

        var existingAssignments = meta.Query<UserRole>()
            .Where(item => item.UserId == userId)
            .ToList(item => item.RoleId)
            .ToHashSet();
        var assignments = distinctRoleIds
            .Where(roleId => !existingAssignments.Contains(roleId))
            .Select(roleId => new UserRole { UserId = userId, RoleId = roleId })
            .ToArray();

        if (assignments.Length > 0)
        {
            meta.Insert(assignments, authUserId);
        }
    }
}
