using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using LimeMeta.Models;
using FreeSql;
using LimeMeta.Logics;
using LimeMeta.Configurations;
using FreeSql.DataAnnotations;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Linq.Expressions;
using LimeMeta.Security;

namespace LimeMeta.Data;

/// <summary>
/// 基础元数据
/// </summary>
public abstract class BaseLimeMeta : ILimeMeta
{
    /// <summary>
    /// SeedPath
    /// </summary>
    public const string SeedPath = "Seed";
    public const string BeforeUpdateSchemaSqlFile = "BeforeUpdateSchema.sql";
    public const string AfterUpdateSchemaSqlFile = "AfterUpdateSchema.sql";

    /// <summary>
    /// YamlDeserializer
    /// </summary>
    public static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

    /// <summary>
    /// YamlSerializer
    /// </summary>
    public static readonly ISerializer YamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

    /// <summary>
    /// JsonSerializerOptions
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// BaseLimeMeta
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    /// <param name="logicManager"></param>
    /// <param name="passwordHasher"></param>
    public BaseLimeMeta(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        ILogicManager logicManager,
        ILimeMetaPasswordHasher passwordHasher)
    {
        Logger = loggerFactory.CreateLogger(GetType());
        ScopeFactory = scopeFactory;
        LogicManager = logicManager;
        PasswordHasher = passwordHasher;
    }

    /// <summary>
    /// Logger
    /// </summary>
    /// <value></value>
    public ILogger Logger { get; }

    /// <summary>
    /// ScopeFactory
    /// </summary>
    public IServiceScopeFactory ScopeFactory { get; }

    /// <summary>
    /// LogicManager
    /// </summary>
    public ILogicManager LogicManager { get; }

    /// <summary>
    /// 密码哈希服务。
    /// </summary>
    protected ILimeMetaPasswordHasher PasswordHasher { get; }

    /// <summary>
    /// GetSeedPath
    /// </summary>
    /// <returns></returns>
    public string GetSeedPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SeedPath);

    /// <summary>
    /// LoadSeed
    /// </summary>
    public void LoadSeed()
    {
        var adminUserId = InitUserRolePerm();

        var mi = GetType().GetMethod(nameof(LoadSeed), [typeof(Guid)])!;
        foreach (var type in LogicManager.ModelTypes)
        {
            Logger.LogInformation("加载种子数据 - {type}", type.Name);
            mi.MakeGenericMethod(type).Invoke(this, [adminUserId]);
        }
    }

    /// <summary>
    /// LoadSeed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="userId"></param>
    public void LoadSeed<T>(Guid userId) where T : BaseObject, new()
    {
        var seed = GetSeedPath();
        var type = typeof(T);

        if (type == typeof(UserRole))
        {
        }

        var path = Path.Combine(seed, $"{type.Name}.yaml");
        if (!File.Exists(path)) return;

        var ver = File.GetLastWriteTimeUtc(path).ToReadableLong();
        if (Query<T>().Any() && !Query<T>().Any(r => r.Ver < ver))
        {
            return; // 版本号小于文件修改时间，则不加载
        }

        Logger.LogInformation("{name}: {path}", nameof(LoadSeed), path);

        using var reader = File.OpenText(path);
        var objs = YamlDeserializer.Deserialize<List<T>>(reader);
        if (objs == null) return;

        var insertObjs = new List<T>();
        var updateObjs = new List<T>();
        foreach (var obj in objs)
        {
            // 根据 Id 查找
            var oldObj = Query<T>().FirstOrDefault(r => r.Id == obj.Id);

            if (oldObj == null)
            {
                insertObjs.Add(obj);
            }
            else if (oldObj.Ver < ver)
            {
                obj.Id = oldObj.Id;
                updateObjs.Add(obj);
            }
        }

        Insert(insertObjs, userId);
        Update(updateObjs, null, userId);
    }

    /// <summary>
    /// InitUserRolePerm
    /// </summary>
    /// <returns></returns>
    public Guid InitUserRolePerm()
    {
        Logger.LogInformation("初始化用户、角色、权限...");

        using var sc = ScopeFactory.CreateScope();
        var cfg = sc.ServiceProvider.GetRequiredService<IOptions<LimeMetaConfiguration>>().Value;

        // 管理员
        var adminPerm = Query<Perm>().FirstOrDefault(r => r.Name == cfg.AdminPerm);
        if (adminPerm == null)
        {
            adminPerm = new Perm
            {
                Name = cfg.AdminPerm,
                Sn = 0,
            };

            Insert(new[] { adminPerm }, null, false);
        }

        var adminRole = Query<Role>().FirstOrDefault(r => r.Name == cfg.AdminPerm);
        if (adminRole == null)
        {
            adminRole = new Role
            {
                Name = cfg.AdminPerm,
                Sn = 0,
            };
            Insert(new[] { adminRole }, null, false);
        }

        var adminRolePerm = Query<RolePerm>().FirstOrDefault(r => r.RoleId == adminRole.Id && r.PermId == adminPerm.Id);
        if (adminRolePerm == null)
        {
            adminRolePerm = new RolePerm
            {
                RoleId = adminRole.Id,
                PermId = adminPerm.Id,
            };
            Insert(new[] { adminRolePerm }, null, false);
        }

        var adminUser = Query<User>().FirstOrDefault(r => r.Username == cfg.AdminUserName);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Name = "管理员",
                Username = cfg.AdminUserName,
                PasswordHash = PasswordHasher.HashPassword(cfg.AdminUserPassword),
            };

            Insert(new[] { adminUser }, null, false);
        }

        var adminUserRole = Query<UserRole>().FirstOrDefault(r => r.UserId == adminUser.Id && r.RoleId == adminRole.Id);
        if (adminUserRole == null)
        {
            adminUserRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
            };
            Insert(new[] { adminUserRole }, null, false);
        }

        return adminUser.Id;
    }

    /// <summary>
    /// Query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public abstract ISelect<T> Query<T>() where T : class;

    /// <summary>
    /// Query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <returns></returns>
    public abstract ISelect<T> Query<T>(Type modelType) where T : class;

    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract int Update<T>(Type modelType, List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : class;

    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract int Insert<T>(Type modelType, List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : class;

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <param name="exp"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract int Delete<T>(Type modelType, Expression<Func<T, bool>> exp, Guid? userId = null, bool enableLogic = true, object? context = null) where T : class;

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    public abstract int Insert<T>(List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    public int Insert<T>(IEnumerable<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject => Insert(objs.ToList(), userId, enableLogic, context);

    /// <summary>
    /// InsertAsync
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract Task<int> InsertAsync<T>(List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// ExecSql
    /// </summary>
    /// <param name="sql"></param>
    public abstract void ExecSql(string sql);

    /// <summary>
    /// UpdateSchema
    /// </summary>
    public abstract void UpdateSchema();

    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="fields"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract int Update<T>(List<T> objs, IEnumerable<string>? fields = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="fields"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public int Update<T>(IEnumerable<T> objs, IEnumerable<string>? fields = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject => Update(objs.ToList(), fields, userId, enableLogic, context);

    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public int Update<T>(IEnumerable<JObject> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject
    {
        if (!objs.Any()) return 0;

        var newJsons = objs.ToDictionary(r => (Guid)r["id"]!);
        var ids = newJsons.Keys.ToList();
        var oldObjs = Query<T>().Where(r => ids.Contains(r.Id)).ToList();

        var dict = new Dictionary<T, T>();
        foreach (var oldObj in oldObjs)
        {
            var newJson = newJsons[oldObj.Id];

            var newObj = (T)oldObj.Clone();
            newObj.Merge(newJson);

            dict[oldObj] = newObj;
        }

        return Update(dict.Values, null, userId, enableLogic, context);
    }

    /// <summary>
    /// Delete
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="exp"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract int Delete<T>(Expression<Func<T, bool>> exp, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Select
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="page"></param>
    /// <param name="includes"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract PageResult<T> Select<T>(ISelect<T> query, PageModel page, IEnumerable<IncludeField>? includes = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Aggr
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="fields"></param>
    /// <param name="groups"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract JArray Aggr<T>(ISelect<T> query, IEnumerable<AggrField> fields, IEnumerable<string>? groups = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;
}
