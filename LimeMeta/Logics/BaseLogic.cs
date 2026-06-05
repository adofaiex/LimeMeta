using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LimeMeta.Models;
using LimeMeta.Data;
using FreeSql;

namespace LimeMeta.Logics;

/// <summary>
/// 基础逻辑
/// </summary>
public abstract class BaseLogic : ILogic
{
    /// <summary>
    /// 序号
    /// </summary>
    /// <value></value>
    public float Order { get; protected set; } = 1;

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
    /// 模型类型
    /// </summary>
    /// <value></value>
    public abstract Type LogicModelType { get; }

    /// <summary>
    /// BaseLogic
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    public BaseLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
        ScopeFactory = scopeFactory;
    }

    public abstract void InvokeBeforeSelect<T>(BeforeSelectEventArgs<T> args) where T : class;
    public abstract void InvokeAfterSelect<T>(AfterSelectEventArgs<T> args) where T : class;
    public abstract void InvokeBeforeInsert<T>(BeforeInsertEventArgs<T> args) where T : class;
    public abstract void InvokeAfterInsert<T>(AfterInsertEventArgs<T> args) where T : class;
    public abstract void InvokeBeforeUpdate<T>(BeforeUpdateEventArgs<T> args) where T : class;
    public abstract void InvokeAfterUpdate<T>(AfterUpdateEventArgs<T> args) where T : class;
    public abstract void InvokeBeforeDelete<T>(BeforeDeleteEventArgs<T> args) where T : class;
    public abstract void InvokeAfterDelete<T>(AfterDeleteEventArgs<T> args) where T : class;
    public abstract void InvokeCreated(CreatedEventArgs args);
}

/// <summary>
/// 基础对象逻辑泛型
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseLogic<T> : BaseLogic where T : class
{
    /// <summary>
    /// BaseLogic
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    public BaseLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        var type = typeof(T);
        if (type.IsClass && !type.IsAbstract)
        {
            Order = 100;
        }
    }

    /// <summary>
    /// 逻辑模型类型
    /// </summary>
    /// <value></value>
    public override Type LogicModelType => typeof(T);

    public event EventHandler<BeforeSelectEventArgs<T>>? BeforeSelect;
    /// <summary>
    /// InvokeBeforeSelect
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeBeforeSelect<Tm>(BeforeSelectEventArgs<Tm> args)
    {
        if (args is BeforeSelectEventArgs<T> args1)
        {
            BeforeSelect?.Invoke(this, args1);
        }
        else
        {
            args1 = new BeforeSelectEventArgs<T>(args.LimeMeta, args.ModelType, args.LimeMeta.Query<T>(args.ModelType), args.UserId, args.Context);
            BeforeSelect?.Invoke(this, args1);
        }
    }

    public event EventHandler<AfterSelectEventArgs<T>>? AfterSelect;
    /// <summary>
    /// InvokeAfterSelect
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeAfterSelect<Tm>(AfterSelectEventArgs<Tm> args)
    {
        if (args is AfterSelectEventArgs<T> args1)
        {
            AfterSelect?.Invoke(this, args1);
        }
        else
        {
            args1 = new AfterSelectEventArgs<T>(args.LimeMeta, args.ModelType, [.. args.Objs.Cast<T>()], args.UserId, args.Context);
            AfterSelect?.Invoke(this, args1);
        }
    }

    public event EventHandler<BeforeInsertEventArgs<T>>? BeforeInsert;
    /// <summary>
    /// InvokeBeforeInsert
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeBeforeInsert<Tm>(BeforeInsertEventArgs<Tm> args)
    {
        if (args is BeforeInsertEventArgs<T> args1)
        {
            BeforeInsert?.Invoke(this, args1);
        }
        else
        {
            args1 = new BeforeInsertEventArgs<T>(args.LimeMeta, args.ModelType, [.. args.Objs.Cast<T>()], args.UserId, args.Context);
            BeforeInsert?.Invoke(this, args1);
        }
    }

    public event EventHandler<AfterInsertEventArgs<T>>? AfterInsert;
    /// <summary>
    /// InvokeAfterInsert
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeAfterInsert<Tm>(AfterInsertEventArgs<Tm> args)
    {
        if (args is AfterInsertEventArgs<T> args1)
        {
            AfterInsert?.Invoke(this, args1);
        }
        else
        {
            args1 = new AfterInsertEventArgs<T>(args.LimeMeta, args.ModelType, [.. args.Objs.Cast<T>()], args.UserId, args.Context);
            AfterInsert?.Invoke(this, args1);
        }
    }

    public event EventHandler<BeforeUpdateEventArgs<T>>? BeforeUpdate;
    /// <summary>
    /// InvokeBeforeUpdate
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeBeforeUpdate<Tm>(BeforeUpdateEventArgs<Tm> args)
    {
        if (args is BeforeUpdateEventArgs<T> args1)
        {
            BeforeUpdate?.Invoke(this, args1);
        }
        else
        {
            //var dict = args.Objs.ToDictionary(o => (T)Convert.ChangeType(o.Key, typeof(T)), o => (T)Convert.ChangeType(o.Value, typeof(T)));
            var dict = args.Objs.ToDictionary(o => (T)(object)o.Key, o => (T)(object)o.Value);
            args1 = new BeforeUpdateEventArgs<T>(args.LimeMeta, args.ModelType, dict, args.UserId, args.Context);
            BeforeUpdate?.Invoke(this, args1);
        }
    }

    public event EventHandler<AfterUpdateEventArgs<T>>? AfterUpdate;
    /// <summary>
    /// InvokeAfterUpdate
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeAfterUpdate<Tm>(AfterUpdateEventArgs<Tm> args)
    {
        if (args is AfterUpdateEventArgs<T> args1)
        {
            AfterUpdate?.Invoke(this, args1);
        }
        else
        {
            //var dict = args.Objs.ToDictionary(o => (T)Convert.ChangeType(o.Key, typeof(T)), o => (T)Convert.ChangeType(o.Value, typeof(T)));
            var dict = args.Objs.ToDictionary(o => (T)(object)o.Key, o => (T)(object)o.Value);
            args1 = new AfterUpdateEventArgs<T>(args.LimeMeta, args.ModelType, dict, args.UserId, args.Context);
            AfterUpdate?.Invoke(this, args1);
        }
    }

    public event EventHandler<BeforeDeleteEventArgs<T>>? BeforeDelete;
    /// <summary>
    /// InvokeBeforeDelete
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeBeforeDelete<Tm>(BeforeDeleteEventArgs<Tm> args)
    {
        if (args is BeforeDeleteEventArgs<T> args1)
        {
            BeforeDelete?.Invoke(this, args1);
        }
        else
        {
            args1 = new BeforeDeleteEventArgs<T>(args.LimeMeta, args.ModelType, [.. args.Objs.Cast<T>()], args.UserId, args.Context);
            BeforeDelete?.Invoke(this, args1);
        }
    }

    public event EventHandler<AfterDeleteEventArgs<T>>? AfterDelete;
    /// <summary>
    /// InvokeAfterDelete
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override void InvokeAfterDelete<Tm>(AfterDeleteEventArgs<Tm> args)
    {
        if (args is AfterDeleteEventArgs<T> args1)
        {
            AfterDelete?.Invoke(this, args1);
        }
        else
        {
            args1 = new AfterDeleteEventArgs<T>(args.LimeMeta, args.ModelType, [.. args.Objs.Cast<T>()], args.UserId, args.Context);
            AfterDelete?.Invoke(this, args1);
        }
    }

    public event EventHandler<CreatedEventArgs>? Created;
    /// <summary>
    /// InvokeCreated
    /// </summary>
    /// <param name="args"></param>
    public override void InvokeCreated(CreatedEventArgs args) => Created?.Invoke(this, args);
}


/// <summary>
/// BaseEventArgs
/// </summary>
public abstract class BaseEventArgs : EventArgs
{
    public BaseEventArgs(ILimeMeta meta, Type modelType, Guid? userId, object? context)
    {
        LimeMeta = meta;
        UserId = userId;
        Context = context;
        ModelType = modelType;
    }

    public ILimeMeta LimeMeta { get; }
    public Guid? UserId { get; }
    public object? Context { get; }
    public Type ModelType { get; }
}

/// <summary>
/// BeforeSelectEventArgs
/// </summary>
/// <typeparam name="T"></typeparam>
public class BeforeSelectEventArgs<T> : BaseEventArgs where T : class
{
    public BeforeSelectEventArgs(ILimeMeta meta, Type modelType, ISelect<T> q, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Query = q;
    }

    public ISelect<T> Query { get; set; }
}

/// <summary>
/// AfterSelectEventArgs
/// </summary>
/// <typeparam name="T"></typeparam>
public class AfterSelectEventArgs<T> : BaseEventArgs where T : class
{
    public AfterSelectEventArgs(ILimeMeta meta, Type modelType, List<T> objs, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = objs;
    }

    public List<T> Objs { get; }
}

/// <summary>
/// BeforeInsertEventArgs
/// </summary>
public class BeforeInsertEventArgs<T> : BaseEventArgs where T : class
{
    public BeforeInsertEventArgs(ILimeMeta meta, Type modelType, List<T> objs, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = objs;
    }

    /// <summary>
    /// Items
    /// </summary>
    public List<T> Objs { get; }
}

/// <summary>
/// AfterInsertEventArgs
/// </summary>
public class AfterInsertEventArgs<T> : BaseEventArgs where T : class
{
    public AfterInsertEventArgs(ILimeMeta meta, Type modelType, List<T> objs, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = objs;
    }

    /// <summary>
    /// Items
    /// </summary>
    public List<T> Objs { get; }
}


/// <summary>
/// BeforeUpdateEventArgs
/// </summary>
public class BeforeUpdateEventArgs<T> : BaseEventArgs where T : class
{
    public BeforeUpdateEventArgs(ILimeMeta meta, Type modelType, Dictionary<T, T> objs, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = objs;
    }

    /// <summary>
    /// Items
    /// </summary>
    public Dictionary<T, T> Objs { get; }
}

/// <summary>
/// AfterUpdateEventArgs
/// </summary>
public class AfterUpdateEventArgs<T> : BaseEventArgs where T : class
{
    public AfterUpdateEventArgs(ILimeMeta meta, Type modelType, Dictionary<T, T> objs, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = objs;
    }

    /// <summary>
    /// Items
    /// </summary>
    public Dictionary<T, T> Objs { get; }
}

/// <summary>
/// BeforeDeleteEventArgs
/// </summary>
public class BeforeDeleteEventArgs<T> : BaseEventArgs where T : class
{
    public BeforeDeleteEventArgs(ILimeMeta meta, Type modelType, List<T> objs, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = objs;
    }

    /// <summary>
    /// Items
    /// </summary>
    public List<T> Objs { get; }
}

/// <summary>
/// AfterDeleteEventArgs
/// </summary>
public class AfterDeleteEventArgs<T> : BaseEventArgs where T : class
{
    public AfterDeleteEventArgs(ILimeMeta meta, Type modelType, List<T> obj, Guid? userId, object? context) : base(meta, modelType, userId, context)
    {
        Objs = obj;
    }

    /// <summary>
    /// Items
    /// </summary>
    public List<T> Objs { get; }
}

/// <summary>
/// CreatedEventArgs
/// </summary>
public class CreatedEventArgs : EventArgs
{
    /// <summary>
    /// CreatedEventArgs
    /// </summary>
    /// <param name="logicManager"></param>
    public CreatedEventArgs(ILogicManager logicManager)
    {
        LogicManager = logicManager;
    }

    /// <summary>
    /// LogicManager
    /// </summary>
    public ILogicManager LogicManager { get; }
}
