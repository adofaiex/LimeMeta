using FastEndpoints.Security;
using FreeSql;
using LimeMeta.Attributes;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace LimeMeta.Logics;

/// <summary>
/// UserLogic
/// </summary>
public sealed class UserLogic : BaseLogic<User>
{
    /// <summary>
    /// ClaimUserId
    /// </summary>
    public const string ClaimUserId = "meta-user-id";

    /// <summary>
    /// UserLogic
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    public UserLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        AfterSelect += OnAfterSelect;

        BeforeInsert += OnBeforeInsert;
        AfterInsert += OnAfterInsert;
        BeforeDelete += OnBeforeDelete;
    }

    /// <summary>
    /// OnAfterSelect
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAfterSelect(object? sender, AfterSelectEventArgs<User> e)
    {
        foreach (var user in e.Objs)
        {
            user.Password = "*";
        }
    }

    /// <summary>
    /// OnBeforeInsert
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnBeforeInsert(object? sender, BeforeInsertEventArgs<User> args)
    {
        using var sc = args.LimeMeta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        foreach (var user in args.Objs)
        {
            if (string.IsNullOrEmpty(user.Username))
            {
                throw new Exception($"[username]不能为空");
            }

            var oldUser = args.LimeMeta.Query<User>().FirstOrDefault(r => r.Username == user.Username);
            if (oldUser != null)
            {
                throw new Exception($"[{user.Username}]用户已存在");
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                user.Password = config.DefaultUserPassword.GetMD5();
            }
        }
    }

    /// <summary>
    /// OnAfterInsert
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnAfterInsert(object? sender, AfterInsertEventArgs<User> args)
    {
        var guestRole = args.LimeMeta.Query<Role>()
            .FirstOrDefault(r => r.Name == RoleName.Guest);

        var userRoles = new List<UserRole>();
        foreach (var user in args.Objs)
        {
            var role = args.LimeMeta.Query<UserRole>()
                .FirstOrDefault(r => r.UserId == user.Id);
            if (role == null && guestRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = guestRole.Id
                };
                userRoles.Add(userRole);
            }
        }

        if (userRoles.Count > 0)
        {
            args.LimeMeta.Insert(userRoles, args.UserId);
        }
    }

    /// <summary>
    /// OnBeforeDelete
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnBeforeDelete(object? sender, BeforeDeleteEventArgs<User> args)
    {
        var ids = new List<Guid>();
        foreach (var obj in args.Objs)
        {
            ids.Add(obj.Id);
        }
        args.LimeMeta.Delete<DeptUser>(r => ids.Contains(r.UserId), args.UserId);
        args.LimeMeta.Delete<UserRole>(r => ids.Contains(r.UserId), args.UserId);
    }

    /// <summary>
    /// CryptPassword
    /// </summary>
    /// <param name="txt"></param>
    /// <param name="salt"></param>
    /// <returns></returns>
    public static string Salt(string txt, string salt)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(txt, salt);
        return hash[salt.Length..];
    }

    /// <summary>
    /// VerifyPassword
    /// </summary>
    /// <param name="pwd"></param>
    /// <param name="hash"></param>
    /// <param name="salt"></param>
    /// <returns></returns>
    public static bool VerifyPassword(string pwd, string hash, string salt) => BCrypt.Net.BCrypt.Verify(pwd, $"{salt}{hash}");

    /// <summary>
    /// ResetPassword
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="authUserId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static string ResetPassword(ILimeMeta meta, Guid authUserId, Guid userId)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var perms = GetPerms(meta, authUserId);
        var isAdmin = perms.Any(r => r.Name == config.AdminPerm);

        if (!isAdmin)
        {
            throw new Exception("权限不足");
        }

        var user = meta.Query<User>().FirstOrDefault(r => r.Id == userId);
        if (user == null)
        {
            throw new Exception($"[{userId}]用户不存在");
        }

        user.Password = user.Username.GetMD5();
        meta.Update(new[] { user }, null, authUserId);

        return user.Username;
    }

    /// <summary>
    /// ChangePassword
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="authUserId"></param>
    /// <param name="userId"></param>
    /// <param name="oldSalt"></param>
    /// <param name="newHash"></param>
    /// <returns></returns>
    public static string ChangePassword(ILimeMeta meta, Guid authUserId, Guid userId, string oldSalt, string newHash)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var isAdmin = IsAdmin(meta, authUserId);

        if (!isAdmin && authUserId != userId)
        {
            throw new Exception("权限不足");
        }

        if (string.IsNullOrEmpty(newHash) || string.IsNullOrEmpty(oldSalt))
        {
            throw new Exception("密码不能为空");
        }

        var user = meta.Query<User>().Where(r => r.Id == userId).First();
        if (user == null)
        {
            throw new Exception($"[{userId}]用户不存在");
        }

        if (!VerifyPassword(user.Password, oldSalt, config.Salt))
        {
            throw new Exception($"密码验证失败");
        }

        user.Password = newHash;
        meta.Update(new[] { user }, null, authUserId);

        return Salt(newHash, config.Salt);
    }

    /// <summary>
    /// BeforeLoginEventArgs
    /// </summary>
    public class BeforeLoginEventArgs : EventArgs
    {
        public BeforeLoginEventArgs(ILimeMeta meta, string userName, string password, object? context = null)
        {
            UserName = userName;
            Password = password;
            Context = context;
            LimeMeta = meta;
        }

        public ILimeMeta LimeMeta { get; }
        public string UserName { get; }
        public string Password { get; }
        public object? Context { get; }
        public bool Cancel { get; set; } = false;
    }

    public static event EventHandler<BeforeLoginEventArgs>? BeforeLogin;


    /// <summary>
    /// AfterLoginEventArgs
    /// </summary>
    public class AfterLoginEventArgs : EventArgs
    {
        public AfterLoginEventArgs(ILimeMeta meta, User user, string token, object? context = null)
        {
            User = user;
            Token = token;
            Context = context;
            LimeMeta = meta;
        }

        public ILimeMeta LimeMeta { get; }
        public User User { get; }
        public string Token { get; }
        public object? Context { get; }
    }

    public static event EventHandler<AfterLoginEventArgs>? AfterLogin;

    /// <summary>
    /// Login
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static LoginResult Login(ILimeMeta meta, string username, string password, object? context = null)
    {
        if (BeforeLogin != null)
        {
            var args = new BeforeLoginEventArgs(meta, username, password, context);
            BeforeLogin.Invoke(null, args);
        }

        var resp = new LoginResult();

        var user = meta.Query<User>().FirstOrDefault(r => r.Username == username);
        if (user == null) return resp;

        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        if (!VerifyPassword(user.Password!, password!, config.Salt)) return resp;

        resp.Name = user.Name;
        resp.Token = GenerateJwt(config, user);

        if (AfterLogin != null)
        {
            var args = new AfterLoginEventArgs(meta, user, resp.Token!, context);
            AfterLogin.Invoke(null, args);
        }

        return resp;
    }

    /// <summary>
    /// GeneratingJwtEventArgs
    /// </summary>
    public class GeneratingJwtEventArgs : EventArgs
    {
        public GeneratingJwtEventArgs(JwtCreationOptions opt)
        {
            Options = opt;
        }

        public JwtCreationOptions Options { get; }
    }

    public static event EventHandler<GeneratingJwtEventArgs>? GeneratingJwt;

    /// <summary>
    /// GenerateJwt
    /// </summary>
    /// <param name="config"></param>
    /// <param name="user"></param>
    /// <param name="expires"></param>
    /// <param name="claims"></param>
    /// <returns></returns>
    public static string GenerateJwt(LimeMetaConfiguration config, User user, DateTime? expires = null, IEnumerable<Claim>? claims = null)
    {
        var key = Encoding.UTF8.GetBytes(config.JwtSignKey);
        if (expires == null)
        {
            expires = DateTime.Now.AddMilliseconds(config.JwtExpires);
        }

        var jwt = JwtBearer.CreateToken(opt =>
        {
            opt.SigningKey = config.JwtSignKey;
            opt.ExpireAt = DateTime.UtcNow.AddMilliseconds(config.JwtExpires);
            opt.User.Claims.Add(new Claim(ClaimUserId, user.Id.ToString()));
            if (claims != null)
            {
                opt.User.Claims.AddRange(claims);
            }

            // 事件，可以给外部扩展JWT内容
            if (GeneratingJwt != null)
            {
                var args = new GeneratingJwtEventArgs(opt);
                GeneratingJwt.Invoke(null, args);
            }
        });

        return jwt;
    }

    /// <summary>
    /// GetRoles
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static IEnumerable<Role> GetRoles(ILimeMeta meta, Guid userId)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var user = meta.Query<User>().FirstOrDefault(r => r.Id == userId);
        if (user == null)
        {
            return [];
        }

        if (user.Username == config.AdminUserName)
        {
            return meta.Query<Role>().ToList();
        }

        // 用户角色
        var userRoles = meta.Query<UserRole>()
            .Where(r => r.UserId == userId)
            .Include(r => r.Role)
            .Distinct()
            .ToList(r => r.Role!);

        // 部门角色
        var depts = meta.Query<DeptUser>()
            .Where(r => r.UserId == userId)
            .Include(r => r.Dept)
            .Distinct()
            .ToList(r => r.Dept!);

        // 子部门
        var childDepts = new List<Dept>();
        foreach (var dept in depts)
        {
            var childs = meta.Query<Dept>()
                .Where(r => r.Path!.StartsWith(dept.Path!))
                .ToList();
            childDepts.AddRange(childs);
        }

        var deptIds = depts.Union(childDepts).Select(r => r.Id).Distinct().ToList();
        var deptRoles = meta.Query<DeptRole>()
            .Where(r => deptIds.Contains(r.DeptId))
            .Include(r => r.Role)
            .Distinct()
            .ToList(r => r.Role!);

        var roles = userRoles.Union(deptRoles).DistinctBy(r => r!.Id).ToList();
        // 子角色
        var childRoles = new List<Role>();
        foreach (var role in roles)
        {
            var childs = meta.Query<Role>()
                .Where(r => r.Path!.StartsWith(role!.Path!))
                .ToList();
            childRoles.AddRange(childs);
        }

        roles = [.. roles.Union(childRoles).DistinctBy(r => r.Id)];
        return roles;
    }

    /// <summary>
    /// IsAdmin
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static bool IsAdmin(ILimeMeta meta, Guid userId)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var perms = GetPerms(meta, userId);
        if (perms.Any(r => r.Name == config.AdminPerm))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// GetPerms
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static IEnumerable<Perm> GetPerms(ILimeMeta meta, Guid userId)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var user = meta.Query<User>().FirstOrDefault(r => r.Id == userId);
        if (user == null)
        {
            return [];
        }

        if (user.Username == config.AdminUserName)
        {
            return meta.Query<Perm>().ToList();
        }

        // 获取用户角色
        var roleIds = GetRoles(meta, userId).Select(r => r.Id).ToList();
        var rolePermIds = meta.Query<RolePerm>()
            .Where(r => roleIds.Contains(r.RoleId))
            .Distinct()
            .ToList(r => r.PermId);

        var perms = meta.Query<Perm>()
            .Where(r => rolePermIds.Contains(r.Id))
            .ToList();

        if (perms.Any(r => r.Name == config.AdminPerm))
        {
            perms = meta.Query<Perm>().ToList();
        }

        return perms;
    }
}

/// <summary>
/// LoginResult
/// </summary>
public class LoginResult
{
    public string? Name { get; set; }
    public string? Token { get; set; }
}

