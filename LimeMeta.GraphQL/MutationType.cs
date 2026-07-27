using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LimeMeta.Data;
using LimeMeta.Authorization;
using LimeMeta.Logics;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using LimeMeta.Attributes;

namespace LimeMeta.GraphQL;
/// <summary>
/// MutationType
/// </summary>
internal sealed class MutationType : ObjectType<Mutation>
{
    /// <summary>
    /// LogicManager
    /// </summary>
    /// <value></value>
    public ILogicManager LogicManager { get; }

    /// <summary>
    /// Logger
    /// </summary>
    /// <value></value>
    public ILogger<MutationType> Logger { get; }

    /// <summary>
    /// MutationType
    /// </summary>
    /// <param name="logicManager"></param>
    /// <param name="loggerFactory"></param>
    public MutationType(ILogicManager logicManager, ILoggerFactory loggerFactory)
    {
        LogicManager = logicManager;
        Logger = loggerFactory.CreateLogger<MutationType>();
    }

    /// <summary>
    /// Configure
    /// </summary>
    /// <param name="desc"></param>
    protected override void Configure(IObjectTypeDescriptor<Mutation> desc)
    {
        desc.Authorize();

        var mi = GetType().GetMethod(nameof(RegisterModel))!;

        var models = LogicManager.ModelTypes;
        foreach (var model in models)
        {
            if (!model.IsSubclassOf(typeof(BaseObject))) continue;
            if (model.IsDefined(typeof(DisableGraphQLAttribute), inherit: true)) continue;

            var dtoType = LogicManager.GetModelDtoType(model);
            mi.MakeGenericMethod(model, dtoType).Invoke(null, [desc, LogicManager, Logger]);
        }
    }

    /// <summary>
    /// RegisterModel
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TDto"></typeparam>
    /// <param name="desc"></param>
    /// <param name="logicManager"></param>
    /// <param name="logger"></param>
    public static void RegisterModel<T, TDto>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
        where TDto : BaseDto, new()
    {
        var type = typeof(T);

        if (type != typeof(Perm) && type != typeof(User))
        {
            Insert<T, TDto>(desc, logicManager, logger);
            Update<T>(desc, logicManager, logger);
            Delete<T>(desc, logicManager, logger);
        }
    }

    /// <summary>
    /// Insert
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TDto"></typeparam>
    /// <param name="desc"></param>
    /// <param name="logicManager"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static void Insert<T, TDto>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
        where TDto : BaseDto, new()
    {
        var type = typeof(T);

        desc.Field($"insert{type.Name}")
            .Authorize()
            .Argument("objs", a => a.Type(typeof(List<TDto>)))
            .Resolve(ctx =>
            {
                var cliam = ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId);
                var userId = Guid.Parse(cliam.Value);

                var meta = ctx.Service<ILimeMeta>();
                var authorization = ctx.Service<ILimeMetaAuthorizationService>();

                var newObjs = new List<T>();
                var objs = ctx.ArgumentValue<List<TDto>>("objs");

                try
                {
                    authorization.EnsureAuthorized(meta, userId, type, LimeMetaOperation.Insert);
                    foreach (var dto in objs)
                    {
                        var obj = logicManager.ModelMapper.Map<TDto, T>(dto);
                        newObjs.Add(obj);
                    }

                    meta.Insert(newObjs, userId, true, ctx);
                    return newObjs.Select(r => r.Id);
                }
                catch (Exception ex)
                {
                    meta.Logger.LogError(ex, "新增异常");
                    ctx.ReportError(ex.Message);
                    return [];
                }
            });
    }


    /// <summary>
    /// Update
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="desc"></param>
    /// <param name="logicManager"></param>
    /// <param name="logger"></param>
    public static void Update<T>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
    {
        var type = typeof(T);

        desc.Field($"update{type.Name}")
            .Authorize()
            .Argument("objs", a => a.Type(typeof(List<JsonElement>)))
            .Resolve(ctx =>
            {
                var objs = ctx.ArgumentValue<List<JsonElement>>("objs");
                var meta = ctx.Service<ILimeMeta>();
                var authorization = ctx.Service<ILimeMetaAuthorizationService>();

                var cliam = ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId);
                var userId = Guid.Parse(cliam.Value);

                int total = 0;
                try
                {
                    authorization.EnsureAuthorized(meta, userId, type, LimeMetaOperation.Update);
                    total = meta.Update<T>(objs.Select(r => JObject.Parse(r.ToString())), userId, true, ctx);
                }
                catch (Exception ex)
                {
                    meta.Logger.LogError(ex, "更新异常");
                    ctx.ReportError(ex.Message);
                }

                return total;
            });
    }

    /// <summary>
    /// Delete
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="desc"></param>
    /// <param name="logicManager"></param>
    /// <param name="logger"></param>
    public static void Delete<T>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
    {
        var type = typeof(T);

        desc.Field($"delete{type.Name}")
            .Authorize()
            .Argument("ids", a => a.Type(typeof(List<Guid>)))
            .Resolve(ctx =>
            {
                var ids = ctx.ArgumentValue<List<Guid>>("ids");

                var cliam = ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId);
                var userId = Guid.Parse(cliam.Value);

                var total = 0;
                var meta = ctx.Service<ILimeMeta>();
                var authorization = ctx.Service<ILimeMetaAuthorizationService>();

                try
                {
                    authorization.EnsureAuthorized(meta, userId, type, LimeMetaOperation.Delete);
                    total = meta.Delete<T>(r => ids.Contains(r.Id), userId, true, ctx);
                }
                catch (Exception ex)
                {
                    ctx.ReportError(ex);
                }

                return total;
            });
    }
}

