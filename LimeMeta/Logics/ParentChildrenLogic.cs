using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LimeMeta.Data;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LimeMeta.Logics;

/// <summary>
/// ParentChildrenLogic
/// </summary>
public sealed class ParentChildrenLogic : BaseLogic<IParentChildren>
{
    /// <summary>
    /// PathSeparator
    /// </summary>
    public const string PathSeparator = ".";

    /// <summary>
    /// NamePropertyName
    /// </summary>
    public const string NamePropertyName = "Name";

    /// <summary>
    /// ParentChildrenLogic
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    public ParentChildrenLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        AfterInsert += OnAfterInsert;
        AfterUpdate += OnAfterUpdate;
        BeforeDelete += OnBeforeDelete;
    }

    /// <summary>
    /// OnAfterUpdate
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnAfterUpdate(object? sender, AfterUpdateEventArgs<IParentChildren> args)
    {
        var updateObjs = new List<IParentChildren>();
        foreach (var (oldObj, newObj) in args.Objs)
        {
            newObj.Path = CalcIdPath(args.LimeMeta, newObj, args.ModelType, args.UserId);
            if (args.ModelType.GetProperty(NamePropertyName) != null)
            {
                newObj.NamePath = CalcNamePath(args.LimeMeta, newObj, args.ModelType, args.UserId);
            }
            updateObjs.Add(newObj);
        }

        if (updateObjs.Count != 0)
        {
            args.LimeMeta.Update(args.ModelType, updateObjs);
        }
    }

    /// <summary>
    /// OnAfterInsert
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnAfterInsert(object? sender, AfterInsertEventArgs<IParentChildren> args)
    {
        foreach (var obj in args.Objs)
        {
            obj.Path = CalcIdPath(args.LimeMeta, obj, args.ModelType, args.UserId);
            if (args.ModelType.GetProperty(NamePropertyName) != null)
            {
                obj.NamePath = CalcNamePath(args.LimeMeta, obj, args.ModelType, args.UserId);
            }
        }

        args.LimeMeta.Update(args.ModelType, args.Objs);
    }

    /// <summary>
    /// OnBeforeDelete
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnBeforeDelete(object? sender, BeforeDeleteEventArgs<IParentChildren> e)
    {
        foreach (var obj in e.Objs)
        {
            e.LimeMeta.Delete<IParentChildren>(e.ModelType, r => r.ParentId == obj.Id, e.UserId);
        }
    }

    /// <summary>
    /// CalcIdPath
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="obj"></param>
    /// <param name="modelType"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static string CalcIdPath(ILimeMeta meta, IParentChildren obj, Type modelType, Guid? userId)
    {
        var path = string.Empty;
        if (obj.ParentId.HasValue)
        {
            if (meta.Query<IParentChildren>(modelType).Where(r => r.Id == obj.ParentId.Value).First() is IParentChildren parent)
            {
                path = parent.Path;
                if (string.IsNullOrEmpty(path))
                {
                    path = CalcIdPath(meta, parent, modelType, userId);
                    parent.Path = path;
                    meta.Update(modelType, [parent], userId, false);
                }
            }
        }
        return $"{path}{obj.Id}{PathSeparator}";
    }

    /// <summary>
    /// CalcNamePath
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="obj"></param>
    /// <param name="modelType"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static string CalcNamePath(ILimeMeta meta, IParentChildren obj, Type modelType, Guid? userId)
    {
        var path = string.Empty;
        if (obj.ParentId.HasValue)
        {
            if (meta.Query<IParentChildren>(modelType).Where(r => r.Id == obj.ParentId.Value).First() is IParentChildren parent)
            {
                path = parent.NamePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = CalcNamePath(meta, parent, modelType, userId);
                    parent.NamePath = path;
                    meta.Update(modelType, [parent], userId, false);
                }
            }
        }
        return $"{path}{obj.GetPropertyValue<string>(NamePropertyName)}{PathSeparator}";
    }
}
