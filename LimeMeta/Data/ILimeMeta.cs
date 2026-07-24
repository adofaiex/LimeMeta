using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LimeMeta.Logics;
using LimeMeta.Models;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace LimeMeta.Data;

/// <summary>
/// 元数据接口
/// </summary>
public interface ILimeMeta
{
    /// <summary>
    /// Logger
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// ScopeFactory
    /// </summary>
    IServiceScopeFactory ScopeFactory { get; }

    /// <summary>
    /// LogicManager
    /// </summary>
    ILogicManager LogicManager { get; }

    /// <summary>
    /// UpdateSchema
    /// </summary>
    void UpdateSchema();

    /// <summary>
    /// LoadSeed
    /// </summary>
    void LoadSeed();

    /// <summary>
    /// Query
    /// 注意：此方法不会触发逻辑
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ISelect<T> Query<T>() where T : class;

    /// <summary>
    /// Query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <returns></returns>
    ISelect<T> Query<T>(Type modelType) where T : class;

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
    int Update<T>(Type modelType, List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : class;

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    int Insert<T>(Type modelType, List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : class;

    /// <summary>
    /// Delete
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <param name="exp"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    int Delete<T>(Type modelType, Expression<Func<T, bool>> exp, Guid? userId = null, bool enableLogic = true, object? context = null) where T : class;

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    int Insert<T>(List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    int Insert<T>(IEnumerable<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// InsertAsync
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task<int> InsertAsync<T>(List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

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
    int Update<T>(List<T> objs, IEnumerable<string>? fields = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

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
    int Update<T>(IEnumerable<T> objs, IEnumerable<string>? fields = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    int Update<T>(IEnumerable<JObject> objs, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

    /// <summary>
    /// Delete
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="exp"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    int Delete<T>(Expression<Func<T, bool>> exp, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

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
    PageResult<T> Select<T>(ISelect<T> query, PageModel page, IEnumerable<IncludeField>? includes = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;

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
    JArray Aggr<T>(ISelect<T> query, IEnumerable<AggrField> fields, IEnumerable<string>? groups = null, Guid? userId = null, bool enableLogic = true, object? context = null) where T : BaseObject;
}

/// <summary>
/// IncludeField
/// </summary>
public class IncludeField
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型
    /// </summary>
    public IncludeFieldType Type { get; set; } = IncludeFieldType.Object;

    /// <summary>
    /// 子级
    /// </summary>
    public List<IncludeField> Childs { get; set; } = new List<IncludeField> { };
}

/// <summary>
/// IncludeFieldType
/// </summary>
public enum IncludeFieldType
{
    Object = 0,
    List = 1,
}

/// <summary>
/// PageModel
/// </summary>
public class PageModel
{
    public int Index { get; set; } = 1;
    public int Size { get; set; } = 10;
}

/// <summary>
/// PageResult
/// </summary>
public class PageResult<T>
{
    public int Index { get; set; }
    public int Size { get; set; }

    public int Total { get; set; }
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
}

/// <summary>
/// AggrType
/// </summary>
public enum AggrType
{
    Count,
    Avg,
    Min,
    Max,
    Sum
}

/// <summary>
/// AggrField
/// </summary>
public class AggrField
{
    /// <summary>
    /// 类型
    /// </summary>
    public AggrType Type { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
