using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LimeMeta.Logics;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using FreeSql.DataAnnotations;

namespace LimeMeta.Data;

/// <summary>
/// FreeSql 元数据
/// </summary>
public class FreeSqlLimeMeta : BaseLimeMeta
{
    /// <summary>
    /// FreeSqlLimeMeta
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    /// <param name="freeSql"></param>
    /// <param name="logicManager"></param>
    public FreeSqlLimeMeta(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, IFreeSql freeSql, ILogicManager logicManager) : base(loggerFactory, scopeFactory, logicManager)
    {
        FreeSql = freeSql;
    }

    /// <summary>
    /// FreeSql 实例
    /// </summary>
    public IFreeSql FreeSql { get; }

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override int Insert<T>(List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        if (objs.Count <= 0) return 0;

        if (enableLogic)
        {
            LogicManager.RaiseBeforeInsertEvent(new BeforeInsertEventArgs<T>(this, typeof(T), objs, userId, context));
        }

        var total = FreeSql.Insert(objs).ExecuteAffrows();

        if (enableLogic)
        {
            LogicManager.RaiseAfterInsertEvent(new AfterInsertEventArgs<T>(this, typeof(T), objs, userId, context));
        }

        return total;
    }

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public override int Insert<T>(Type modelType, List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null) //=> FreeSql.Insert<T>().AsType(modelType).AppendData(objs).ExecuteAffrows();
    {
        if (!objs.Any()) return 0;

        if (enableLogic)
        {
            LogicManager.RaiseBeforeInsertEvent(new BeforeInsertEventArgs<T>(this, modelType, objs, userId, context));
        }

        var total = FreeSql.Insert<T>().AsType(modelType).AppendData(objs).ExecuteAffrows();

        if (enableLogic)
        {
            LogicManager.RaiseAfterInsertEvent(new AfterInsertEventArgs<T>(this, modelType, objs, userId, context));
        }

        return total;

    }

    /// <summary>
    /// InsertAsync
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objs"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<int> InsertAsync<T>(List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        if (objs.Count <= 0) return 0;

        if (enableLogic)
        {
            LogicManager.RaiseBeforeInsertEvent(new BeforeInsertEventArgs<T>(this, typeof(T), objs, userId, context));
        }

        var total = await FreeSql.Insert(objs).ExecuteAffrowsAsync();

        if (enableLogic)
        {
            LogicManager.RaiseAfterInsertEvent(new AfterInsertEventArgs<T>(this, typeof(T), objs, userId, context));
        }

        return total;
    }

    /// <summary>
    /// Query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public override ISelect<T> Query<T>() => FreeSql.Select<T>();

    /// <summary>
    /// Query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelType"></param>
    /// <returns></returns>
    public override ISelect<T> Query<T>(Type modelType) => FreeSql.Select<T>().AsType(modelType);

    /// <summary>
    /// ExecSql
    /// </summary>
    /// <param name="sql"></param>
    public override void ExecSql(string sql) => FreeSql.Ado.ExecuteNonQuery(sql);

    /// <summary>
    /// UpdateSchema
    /// </summary>
    public override void UpdateSchema()
    {
        var beforeUpdateSchemaSql = File.ReadAllText(Path.Combine(GetSeedPath(), BeforeUpdateSchemaSqlFile));
        ExecSql(beforeUpdateSchemaSql);

        foreach (var type in LogicManager.ModelTypes)
        {
            Logger.LogInformation("同步表结构 - {name}", type.Name);
            FreeSql.CodeFirst.SyncStructure(type);
        }

        var afterUpdateSchemaSql = File.ReadAllText(Path.Combine(GetSeedPath(), AfterUpdateSchemaSqlFile));
        ExecSql(afterUpdateSchemaSql);
    }

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
    public override int Update<T>(List<T> objs, IEnumerable<string>? fields = null, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        if (objs.Count <= 0) return 0;

        Dictionary<T, T>? dict = null;
        if (enableLogic)
        {
            dict = [];

            var ids = objs.Cast<BaseObject>().Select(o => o.Id).ToArray();
            var oldObjs = Query<T>().Where(r => ids.Contains(r.Id)).ToList();

            foreach (var oldObj in oldObjs)
            {
                var id = (oldObj as BaseObject)!.Id;
                dict[oldObj] = objs.Single(r => r.Id == id);
            }

            LogicManager.RaiseBeforeUpdateEvent(new BeforeUpdateEventArgs<T>(this, typeof(T), dict, userId, context));
        }

        IUpdate<T> update;
        if (dict != null)
        {
            update = FreeSql.Update<T>().SetSource(dict.Values);
        }
        else
        {
            update = FreeSql.Update<T>().SetSource(objs);
        }

        if (fields != null && fields.Any())
        {
            update = update.UpdateColumns(fields.ToArray());
        }

        var total = update.ExecuteAffrows();

        if (enableLogic)
        {
            LogicManager.RaiseAfterUpdateEvent(new AfterUpdateEventArgs<T>(this, typeof(T), dict!, userId, context));
        }

        return total;
    }

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
    public override int Update<T>(Type modelType, List<T> objs, Guid? userId = null, bool enableLogic = true, object? context = null)// => FreeSql.Update<T>().AsType(modelType).SetSource(objs).ExecuteAffrows();
    {
        if (!objs.Any()) return 0;

        Dictionary<T, T>? dict = null;
        if (enableLogic)
        {
            dict = [];

            var ids = objs.Cast<BaseObject>().Select(o => o.Id).ToArray();
            var oldObjs = Query<T>(modelType).WhereDynamic(ids).ToList();

            foreach (var oldObj in oldObjs)
            {
                var id = (oldObj as BaseObject)!.Id;
                dict[oldObj] = objs.Single(r => r.GetPropertyValue<Guid>(nameof(BaseObject.Id)) == id);
            }

            LogicManager.RaiseBeforeUpdateEvent(new BeforeUpdateEventArgs<T>(this, modelType, dict, userId, context));
        }

        IUpdate<T> update;
        if (dict != null)
        {
            update = FreeSql.Update<T>().AsType(modelType).SetSource(dict.Values);
        }
        else
        {
            update = FreeSql.Update<T>().AsType(modelType).SetSource(objs);
        }

        var total = update.ExecuteAffrows();

        if (enableLogic)
        {
            LogicManager.RaiseAfterUpdateEvent(new AfterUpdateEventArgs<T>(this, modelType, dict!, userId, context));
        }

        return total;

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
    public override int Delete<T>(Expression<Func<T, bool>> exp, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        var total = 0;
        List<T>? oldObjs = null;

        if (enableLogic)
        {
            oldObjs = FreeSql.Select<T>().Where(exp).ToList();
            if (!oldObjs.Any()) return 0;

            LogicManager.RaiseBeforeDeleteEvent(new BeforeDeleteEventArgs<T>(this, typeof(T), oldObjs, userId, context));
        }

        if (oldObjs != null)
        {
            var ids = oldObjs.Cast<BaseObject>().Select(x => x.Id).ToArray();
            total = FreeSql.Delete<T>().Where(r => ids.Contains(r.Id)).ExecuteAffrows();
        }
        else
        {
            total = FreeSql.Delete<T>().Where(exp).ExecuteAffrows();
        }

        if (enableLogic)
        {
            LogicManager.RaiseAfterDeleteEvent(new AfterDeleteEventArgs<T>(this, typeof(T), oldObjs!, userId, context));
        }

        return total;
    }

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
    public override int Delete<T>(Type modelType, Expression<Func<T, bool>> exp, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        var total = 0;
        List<T>? oldObjs = null;

        if (enableLogic)
        {
            oldObjs = FreeSql.Select<T>().AsType(modelType).Where(exp).ToList();
            if (!oldObjs.Any()) return 0;

            LogicManager.RaiseBeforeDeleteEvent(new BeforeDeleteEventArgs<T>(this, modelType, oldObjs, userId, context));
        }

        if (oldObjs != null)
        {
            var ids = oldObjs.Cast<BaseObject>().Select(x => x.Id).ToArray();
            total = FreeSql.Delete<T>().AsType(modelType).WhereDynamic(ids).ExecuteAffrows();
        }
        else
        {
            total = FreeSql.Delete<T>().AsType(modelType).Where(exp).ExecuteAffrows();
        }

        if (enableLogic)
        {
            LogicManager.RaiseAfterDeleteEvent(new AfterDeleteEventArgs<T>(this, modelType, oldObjs!, userId, context));
        }

        return total;

    }

    /// <summary>
    /// Select
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="q"></param>
    /// <param name="page"></param>
    /// <param name="includes"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override PageResult<T> Select<T>(ISelect<T> q, PageModel page, IEnumerable<IncludeField>? includes = null, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        if (enableLogic)
        {
            var args = new BeforeSelectEventArgs<T>(this, typeof(T), q, userId, context);
            LogicManager.RaiseBeforeSelectEvent(args);

            q = args.Query;
        }

        var total = (int)q.Count();

        // includes
        if (includes != null && includes.Any())
        {
            q = BuildIncludeQuery(q, includes);
        }

        List<T> objs;
        if (page.Size > 0)
        {
            objs = q.Page(page.Index, page.Size).ToList();
        }
        else
        {
            objs = q.ToList();
        }

        if (enableLogic)
        {
            var args = new AfterSelectEventArgs<T>(this, typeof(T), objs, userId, context);
            LogicManager.RaiseAfterSelectEvent(args);

            objs = args.Objs;
        }

        return new PageResult<T>
        {
            Index = page.Index,
            Size = page.Size,
            Total = total,
            Items = objs
        };
    }

    private static MethodInfo _miIncludeByPropertyName = typeof(ISelect<object>).GetMethod("IncludeByPropertyName", [typeof(string)])!;
    /// <summary>
    /// BuildIncludeQuery
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="q"></param>
    /// <param name="fields"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static ISelect<T> BuildIncludeQuery<T>(ISelect<T> q, IEnumerable<IncludeField> fields, string? prefix = null) where T : BaseObject
    {
        foreach (var field in fields)
        {
            if (field.Childs.Count == 0)
            {
                q = q.IncludeByPropertyName($"{prefix}{field.Name}");
            }
            else
            {
                if (field.Type == IncludeFieldType.Object)
                {
                    q = BuildIncludeQuery<T>(q, field.Childs, $"{field.Name}.");
                }
                else if (field.Type == IncludeFieldType.List)
                {
                    var expParm = Expression.Parameter(typeof(ISelect<object>), "then");
                    MethodCallExpression? expCall = null;
                    foreach (var child in field.Childs)
                    {
                        if (expCall == null)
                        {
                            expCall = Expression.Call(expParm, _miIncludeByPropertyName, Expression.Constant(child.Name));
                            continue;
                        }

                        expCall = Expression.Call(expCall, _miIncludeByPropertyName, Expression.Constant(child.Name));
                    }

                    var exp = Expression.Lambda<Action<ISelect<object>>>(expCall!, expParm)!;
                    q = q.IncludeByPropertyName($"{prefix}{field.Name}", exp);
                }
            }
        }

        return q;
    }

    /// <summary>
    /// Aggr
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="q"></param>
    /// <param name="fields"></param>
    /// <param name="groups"></param>
    /// <param name="userId"></param>
    /// <param name="enableLogic"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override JArray Aggr<T>(ISelect<T> q, IEnumerable<AggrField> fields, IEnumerable<string>? groups = null, Guid? userId = null, bool enableLogic = true, object? context = null)
    {
        if (!fields.Any()) return new JArray();

        if (enableLogic)
        {
            var args = new BeforeSelectEventArgs<T>(this, typeof(T), q, userId, context);
            LogicManager.RaiseBeforeSelectEvent(args);

            q = args.Query;
        }

        var type = typeof(T);
        var pis = type.GetProperties();

        var dictGroup = new Dictionary<string, string>();
        var sbGroup = new StringBuilder();
        if (groups != null && groups.Any())
        {
            foreach (var group in groups)
            {
                var pi = pis.FirstOrDefault(r => r.Name.Equals(group, StringComparison.OrdinalIgnoreCase));
                if (pi == null) continue;

                var colAttr = pi.GetCustomAttribute(typeof(ColumnAttribute), true) as ColumnAttribute;
                if (colAttr == null) continue;

                dictGroup[group] = colAttr.Name;
                sbGroup.Append($"a.{colAttr.Name},");
            }

            if (dictGroup.Any())
            {
                sbGroup.Remove(sbGroup.Length - 1, 1);
                q = q.GroupBy(sbGroup.ToString());
            }
        }

        var sbAggr = new StringBuilder();
        foreach (var field in fields)
        {
            var pi = pis.FirstOrDefault(r => r.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
            if (pi == null) continue;

            var colAttr = pi.GetCustomAttribute(typeof(ColumnAttribute), true) as ColumnAttribute;
            if (colAttr == null) continue;

            if (field.Type == AggrType.Count)
            {
                var piId = pis.FirstOrDefault(r => r.Name.Equals(nameof(BaseObject.Id), StringComparison.OrdinalIgnoreCase));
                if (piId == null) continue;

                var colAttrId = piId.GetCustomAttribute(typeof(ColumnAttribute), true) as ColumnAttribute;
                if (colAttrId == null) continue;

                sbAggr.Append($"{field.Type}(a.{colAttrId.Name}) \"{field.Name}{field.Type}\",");
            }
            else
            {
                sbAggr.Append($"{field.Type}(a.{colAttr.Name}) \"{field.Name}{field.Type}\",");
            }
        }

        foreach (var pair in dictGroup)
        {
            sbAggr.Append($"a.{pair.Value} \"{pair.Key}\",");
        }

        if (sbAggr.Length == 0)
        {
            return new JArray();
        }

        sbAggr.Remove(sbAggr.Length - 1, 1);

        var sql = q.ToSql(sbAggr.ToString());
        var res = FreeSql.Ado.Query<dynamic>(sql);

        return JArray.FromObject(res);
    }
}

