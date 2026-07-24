using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LimeMeta.Attributes;
using FreeSql.DataAnnotations;
using LimeMeta.Models;
using AutoMapper;

namespace LimeMeta.Logics;

/// <summary>
/// 逻辑管理器
/// </summary>
internal sealed class LogicManager : ILogicManager
{
    /// <summary>
    /// Logger
    /// </summary>
    /// <value></value>
    public ILogger Logger { get; }

    /// <summary>
    /// ScopeFactory
    /// </summary>
    public IServiceScopeFactory ScopeFactory { get; }

    private readonly List<Type> _modelTypes;
    /// <summary>
    /// Models
    /// </summary>
    /// <value></value>
    public IEnumerable<Type> ModelTypes => _modelTypes;

    private readonly List<BaseLogic> _logics = [];
    /// <summary>
    /// Logics
    /// </summary>
    /// <value></value>
    public IEnumerable<BaseLogic> Logics => _logics;

    private readonly Dictionary<Type, List<BaseLogic>> _modelLogics = [];

    /// <summary>
    /// ModelMapper
    /// </summary>
    /// <value></value>
    public IMapper ModelMapper { get; private set; } = null!;

    /// <summary>
    /// LogicManager
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="moduleAssemblies"></param>
    public LogicManager(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IServiceProvider serviceProvider,
        IEnumerable<LimeMetaModuleAssembly> moduleAssemblies)
    {
        Logger = loggerFactory.CreateLogger(GetType());
        ScopeFactory = scopeFactory;
        var scanAssemblies = GetScanAssemblies(moduleAssemblies);

        // 获取所有模型类型（避免对第三方程序集全量 GetTypes，部分依赖组合会触发 ReflectionTypeLoadException）
        _modelTypes = [.. scanAssemblies
            .SelectMany(GetLoadableTypes)
            .Where(t => t.GetCustomAttribute<TableAttribute>() != null && t.IsSubclassOf(typeof(BaseObject)))];

        RebuildModelMapper(loggerFactory);

        // 获取所有逻辑类型        
        var logicTypes = scanAssemblies
            .SelectMany(GetLoadableTypes)
            .Where(t => t.IsSubclassOf(typeof(BaseLogic)) && !t.IsAbstract);

        foreach (var logicType in logicTypes)
        {
            var logic = (BaseLogic)ActivatorUtilities.CreateInstance(serviceProvider, logicType, loggerFactory, scopeFactory);
            _logics.Add(logic);
        }

        AssignModelLogics();

        // 触发逻辑创建事件
        foreach (var logic in _logics)
        {
            var args = new CreatedEventArgs(this);
            logic.InvokeCreated(args);
        }
    }

    /// <summary>
    /// 注册业务程序集中的模型和逻辑。
    /// </summary>
    /// <param name="assembly">业务程序集。</param>
    /// <param name="serviceProvider">服务提供者。</param>
    public void RegisterAssembly(Assembly assembly, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var modelChanged = false;
        foreach (var modelType in GetLoadableTypes(assembly)
            .Where(t => t.GetCustomAttribute<TableAttribute>() != null && t.IsSubclassOf(typeof(BaseObject))))
        {
            if (_modelTypes.Contains(modelType)) continue;

            _modelTypes.Add(modelType);
            modelChanged = true;
        }

        if (modelChanged)
        {
            RebuildModelMapper(loggerFactory);
        }

        foreach (var logicType in GetLoadableTypes(assembly)
            .Where(t => t.IsSubclassOf(typeof(BaseLogic)) && !t.IsAbstract))
        {
            if (_logics.Any(logic => logic.GetType() == logicType)) continue;

            var logic = (BaseLogic)ActivatorUtilities.CreateInstance(serviceProvider, logicType, loggerFactory, ScopeFactory);
            _logics.Add(logic);
            logic.InvokeCreated(new CreatedEventArgs(this));
        }

        AssignModelLogics();
    }

    /// <summary>
    /// 分配模型逻辑
    /// </summary>
    private void AssignModelLogics()
    {
        foreach (var modelType in _modelTypes)
        {
            var modelLogics = new List<BaseLogic>();
            foreach (var logic in _logics)
            {
                if (logic.LogicModelType == modelType || modelType.IsSubclassOf(logic.LogicModelType) || modelType.GetInterfaces().Contains(logic.LogicModelType))
                {
                    modelLogics.Add(logic);
                }
            }

            // 排序modelLogics
            modelLogics.Sort((a, b) => a.Order.CompareTo(b.Order));

            _modelLogics[modelType] = modelLogics;
            Logger.LogInformation("模型逻辑：{modelType}\n{logics}", modelType.Name, string.Join("\n", modelLogics.Select(l => $"{l.Order} - {l.GetType().Name}")));
        }
    }

    /// <summary>
    /// 重建模型映射器。
    /// </summary>
    /// <param name="loggerFactory">日志工厂。</param>
    private void RebuildModelMapper(ILoggerFactory loggerFactory)
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            foreach (var modelType in _modelTypes)
            {
                if (!modelType.IsSubclassOf(typeof(BaseObject))) continue;

                var dtoType = GetModelDtoType(modelType);
                cfg.CreateMap(modelType, dtoType);
                cfg.CreateMap(dtoType, modelType);
            }
        }, loggerFactory);
        mapperConfig.CompileMappings();
        ModelMapper = mapperConfig.CreateMapper()!;
    }

    /// <summary>
    /// RaiseBeforeSelectEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseBeforeSelectEvent<T>(BeforeSelectEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseBeforeSelectEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeBeforeSelect(args);
            }
            Logger.LogInformation("<=== RaiseBeforeSelectEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseAfterSelectEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseAfterSelectEvent<T>(AfterSelectEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseAfterSelectEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeAfterSelect(args);
            }
            Logger.LogInformation("<=== RaiseAfterSelectEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseBeforeInsertEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseBeforeInsertEvent<T>(BeforeInsertEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseBeforeInsertEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeBeforeInsert(args);
            }
            Logger.LogInformation("<=== RaiseBeforeInsertEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseAfterInsertEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseAfterInsertEvent<T>(AfterInsertEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseAfterInsertEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeAfterInsert(args);
            }
            Logger.LogInformation("<=== RaiseAfterInsertEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseBeforeUpdateEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseBeforeUpdateEvent<T>(BeforeUpdateEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseBeforeUpdateEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeBeforeUpdate(args);
            }
            Logger.LogInformation("<=== RaiseBeforeUpdateEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseAfterUpdateEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseAfterUpdateEvent<T>(AfterUpdateEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseAfterUpdateEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeAfterUpdate(args);
            }
            Logger.LogInformation("<=== RaiseAfterUpdateEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseBeforeDeleteEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseBeforeDeleteEvent<T>(BeforeDeleteEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseBeforeDeleteEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeBeforeDelete(args);
            }
            Logger.LogInformation("<=== RaiseBeforeDeleteEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// RaiseAfterDeleteEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    public void RaiseAfterDeleteEvent<T>(AfterDeleteEventArgs<T> args) where T : class
    {
        var modelType = args.ModelType;
        if (_modelLogics.TryGetValue(modelType, out var logics))
        {
            Logger.LogInformation("===> RaiseAfterDeleteEvent: {modelType}", modelType.Name);
            foreach (var logic in logics)
            {
                Logger.LogInformation("{order}-{logic}", logic.Order, logic.GetType().Name);
                logic.InvokeAfterDelete(args);
            }
            Logger.LogInformation("<=== RaiseAfterDeleteEvent: {modelType}", modelType.Name);
        }
    }

    /// <summary>
    /// GetLoadableTypes
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        if (assembly.IsDynamic) return [];
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static t => t != null).Cast<Type>();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// 获取需要扫描的程序集。
    /// </summary>
    /// <param name="moduleAssemblies">业务模块程序集。</param>
    /// <returns>程序集列表。</returns>
    private static Assembly[] GetScanAssemblies(IEnumerable<LimeMetaModuleAssembly> moduleAssemblies)
    {
        return [.. AppDomain.CurrentDomain.GetAssemblies()
            .Concat(moduleAssemblies.Select(module => module.Assembly))
            .Where(assembly => !assembly.IsDynamic)
            .Distinct()];
    }

    /// <summary>
    /// GetModelDtoType
    /// </summary>
    /// <param name="modelType"></param>
    /// <returns></returns>
    public Type GetModelDtoType(Type modelType)
    {
        var typeName = $"{modelType.FullName}Dto";
        return modelType.Assembly.GetType(typeName) ?? throw new Exception($"缺少Dto定义: model={modelType.FullName}");
    }

    /// <summary>
    /// GetLogic
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public T GetLogic<T>() where T : BaseLogic
    {
        var logicType = typeof(T);
        var logic = _logics.FirstOrDefault(r => r.GetType() == logicType);
        if (logic != null)
        {
            return (T)logic;
        }

        throw new Exception($"未找到逻辑: {logicType.FullName}");
    }
}
