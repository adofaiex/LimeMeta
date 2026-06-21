using FreeSql;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LimeMeta.Logics;

/// <summary>
/// DeptLogic
/// </summary>
public sealed class DeptLogic : BaseLogic<Dept>
{
    /// <summary>
    /// DeptLogic
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    public DeptLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        BeforeDelete += OnBeforeDelete;
    }

    private void OnBeforeDelete(object? sender, BeforeDeleteEventArgs<Dept> args)
    {
        var ids = args.Objs.Select(r => r.Id).ToList();
        args.LimeMeta.Delete<DeptUser>(r => ids.Contains(r.DeptId), args.UserId);
        args.LimeMeta.Delete<DeptRole>(r => ids.Contains(r.DeptId), args.UserId);
    }

    /// <summary>
    /// GetRoles
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="deptId"></param>
    /// <returns></returns>
    public static IEnumerable<Role> GetRoles(ILimeMeta meta, Guid deptId)
    {
        var dept = meta.Query<Dept>().FirstOrDefault(r => r.Id == deptId);
        if (dept == null)
        {
            return [];
        }

        var deptIds = meta.Query<Dept>()
            .Where(r => r.Path!.StartsWith(dept.Path!))
            .Select(r => r.Id)
            .Distinct()
            .ToList();

        var deptRoles = meta.Query<DeptRole>()
            .Where(r => deptIds.Contains(r.DeptId))
            .Include(r => r.Role)
            .Select(r => r.Role!)
            .Distinct()
            .ToList();

        var roles = new List<Role>();
        foreach (var role in deptRoles)
        {
            var childs = meta.Query<Role>()
                .Where(r => r.Path!.StartsWith(role.Path!))
                .ToList();
            roles.AddRange(childs);
        }

        return roles.DistinctBy(r => r.Id);
    }

    /// <summary>
    /// GetPerms
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="deptId"></param>
    /// <returns></returns>
    public static IEnumerable<Perm> GetPerms(ILimeMeta meta, Guid deptId)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var dept = meta.Query<Dept>().FirstOrDefault(r => r.Id == deptId);
        if (dept == null)
        {
            return [];
        }

        var roleIds = GetRoles(meta, deptId).Select(r => r.Id).ToList();
        var rolePermIds = meta.Query<RolePerm>()
            .Where(r => roleIds.Contains(r.RoleId))
            .Select(r => r.PermId)
            .Distinct()
            .ToList();

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
