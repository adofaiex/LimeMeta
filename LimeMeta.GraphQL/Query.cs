using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LimeMeta.Logics;
using LimeMeta.Data;
using LimeMeta.Models;
using HotChocolate.Resolvers;

namespace LimeMeta.GraphQL;

/// <summary>
/// Query
/// </summary>
public class Query
{
    private readonly ILogicManager _logicManager;

    /// <summary>
    /// Query
    /// </summary>
    /// <param name="logicManager"></param>
    public Query(ILogicManager logicManager)
    {
        _logicManager = logicManager;
    }

    /// <summary>
    /// GetCurrentUserPerm
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [GraphQLName("currUserPerm")]
    public IEnumerable<Perm> GetCurrentUserPerm(IResolverContext ctx)
    {
        var cliam = ctx.GetUser()!.Claims.FirstOrDefault(r => r.Type == UserLogic.ClaimUserId) ?? throw new GraphQLException("用户未认证");
        var userId = Guid.Parse(cliam.Value);

        var meta = ctx.Service<ILimeMeta>();
        return UserLogic.GetPerms(meta, userId).ToArray();
    }

    /// <summary>
    /// GetCurrentUserPerm
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [GraphQLName("currUserRole")]
    public IEnumerable<Role> GetCurrentUserRole(IResolverContext ctx)
    {
        var cliam = ctx.GetUser()!.Claims.FirstOrDefault(r => r.Type == UserLogic.ClaimUserId) ?? throw new GraphQLException("用户未认证");
        var userId = Guid.Parse(cliam.Value);

        var meta = ctx.Service<ILimeMeta>();
        return UserLogic.GetRoles(meta, userId).ToArray();
    }


    /// <summary>
    /// GetCurrentUserPerm
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [GraphQLName("currUserId")]
    public Guid? GetCurrentUserId(IResolverContext ctx)
    {
        var cliam = ctx.GetUser()!.Claims.FirstOrDefault(r => r.Type == UserLogic.ClaimUserId) ?? throw new GraphQLException("用户未认证");
        var userId = Guid.Parse(cliam.Value);

        var meta = ctx.Service<ILimeMeta>();
        var user = meta.Query<User>().FirstOrDefault(r => r.Id == userId);
        if (user == null)
        {
            throw new GraphQLException("用户不存在");
        }

        return userId;
    }

    /// <summary>
    /// GetAllUserRole
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    [GraphQLName("allUserRole")]
    public IEnumerable<Role> GetAllUserRole(IResolverContext ctx, Guid userId)
    {
        var meta = ctx.Service<ILimeMeta>();
        return UserLogic.GetRoles(meta, userId).ToArray();
    }

    /// <summary>
    /// GetAllUserPerm
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    [GraphQLName("allUserPerm")]
    public IEnumerable<Perm> GetAllUserPerm(IResolverContext ctx, Guid userId)
    {
        var meta = ctx.Service<ILimeMeta>();
        return UserLogic.GetPerms(meta, userId).ToArray();
    }

    /// <summary>
    /// GetAllDeptRole
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="deptId"></param>
    /// <returns></returns>
    [GraphQLName("allDeptRole")]
    public IEnumerable<Role> GetAllDeptRole(IResolverContext ctx, Guid deptId)
    {
        var meta = ctx.Service<ILimeMeta>();
        return DeptLogic.GetRoles(meta, deptId).ToArray();
    }

    /// <summary>
    /// GetAllDeptPerm
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="deptId"></param>
    /// <returns></returns>
    [GraphQLName("allDeptPerm")]
    public IEnumerable<Perm> GetAllDeptPerm(IResolverContext ctx, Guid deptId)
    {
        var meta = ctx.Service<ILimeMeta>();
        return DeptLogic.GetPerms(meta, deptId).ToArray();
    }
}
