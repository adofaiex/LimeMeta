using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;
using HotChocolate.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace LimeMeta.GraphQL;

/// <summary>
/// Mutation
/// </summary>
public class Mutation
{
    private readonly ILogicManager _logicManager;

    /// <summary>
    /// Mutation
    /// </summary>
    /// <param name="logicManager"></param>
    public Mutation(ILogicManager logicManager)
    {
        _logicManager = logicManager;
    }

    /// <summary>
    /// Login
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="ctx"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [AllowAnonymous]
    public LoginResult Login([Service] ILimeMeta meta, IResolverContext ctx, string username, string password, string? code)
    {
        return UserLogic.Login(meta, username, password, ctx);
    }

    /// <summary>
    /// ResetPassword
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="ctx"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public PasswordResult ResetPassword([Service] ILimeMeta meta, IResolverContext ctx, Guid userId)
    {
        var result = new PasswordResult();

        var cliam = ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId);
        var authUserId = Guid.Parse(cliam.Value);

        try
        {
            result.Password = UserLogic.ResetPassword(meta, authUserId, userId);
        }
        catch (Exception ex)
        {
            result.Password = string.Empty;
            ctx.ReportError(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// ChangePassword
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="ctx"></param>
    /// <param name="userId"></param>
    /// <param name="oldPassword"></param>
    /// <param name="newHash"></param>
    /// <returns></returns>
    public PasswordResult ChangePassword([Service] ILimeMeta meta, IResolverContext ctx, Guid userId, string oldPassword, string newHash)
    {
        var result = new PasswordResult();

        var cliam = ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId);
        var authUserId = Guid.Parse(cliam.Value);

        try
        {
            result.Password = UserLogic.ChangePassword(meta, authUserId, userId, oldPassword, newHash);
        }
        catch (Exception ex)
        {
            result.Password = string.Empty;
            ctx.ReportError(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Crypt
    /// </summary>
    /// <param name="config"></param>
    /// <param name="txt"></param>
    /// <returns></returns>
    [AllowAnonymous]
    public CryptResult Crypt([Service] LimeMetaConfiguration config, string txt)
    {
        var md5 = txt.GetMD5();
        var result = new CryptResult
        {
            Hash = md5,
            Salt = UserLogic.Salt(md5, config.Salt)
        };

        return result;
    }
}

/// <summary>
/// PasswordResult
/// </summary>
public class PasswordResult
{
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// CryptResult
/// </summary>
public class CryptResult
{
    public string Hash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
}


