using System.Reflection;
using System.Text.Json;
using LimeMeta.Attributes;
using LimeMeta.Authorization;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace LimeMeta.GraphQL;

/// <summary>
/// MutationType
/// </summary>
internal sealed class MutationType : ObjectType<Mutation>
{
    public ILogicManager LogicManager { get; }

    public ILogger<MutationType> Logger { get; }

    public MutationType(ILogicManager logicManager, ILoggerFactory loggerFactory)
    {
        LogicManager = logicManager;
        Logger = loggerFactory.CreateLogger<MutationType>();
    }

    protected override void Configure(IObjectTypeDescriptor<Mutation> desc)
    {
        desc.Authorize();

        var mi = GetType().GetMethod(nameof(RegisterModel))!;

        foreach (var model in LogicManager.ModelTypes)
        {
            if (!model.IsSubclassOf(typeof(BaseObject))) continue;
            if (model.GetCustomAttribute<LimeMetaIgnoreGraphQLAttribute>() != null) continue;

            var dtoType = LogicManager.GetModelDtoType(model);
            mi.MakeGenericMethod(model, dtoType).Invoke(null, [desc, LogicManager, Logger]);
        }
    }

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

    public static void Insert<T, TDto>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
        where TDto : BaseDto, new()
    {
        var type = typeof(T);

        desc.Field($"insert{type.Name}")
            .Argument("objs", a => a.Type<NonNullType<ListType<NonNullType<InputObjectType<TDto>>>>>())
            .Type<NonNullType<ListType<NonNullType<UuidType>>>>()
            .Resolve(async ctx =>
            {
                var userId = Guid.Parse(ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId).Value);
                var meta = ctx.Service<ILimeMeta>();
                ctx.Service<ILimeMetaAuthorizationService>()
                    .EnsureAuthorized(meta, userId, typeof(T), LimeMetaOperation.Insert);

                var dtos = ctx.ArgumentValue<List<TDto>>("objs");
                var objs = logicManager.ModelMapper.Map<List<T>>(dtos);
                meta.Insert(objs, userId, true, ctx);
                return objs.Select(o => o.Id).ToList();
            });
    }

    public static void Update<T>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
    {
        var type = typeof(T);

        desc.Field($"update{type.Name}")
            .Argument("objs", a => a.Type<NonNullType<ListType<NonNullType<AnyType>>>>())
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var userId = Guid.Parse(ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId).Value);
                var meta = ctx.Service<ILimeMeta>();
                ctx.Service<ILimeMetaAuthorizationService>()
                    .EnsureAuthorized(meta, userId, typeof(T), LimeMetaOperation.Update);

                var raw = ctx.ArgumentValue<List<object>>("objs");
                var jobjs = raw.Select(r =>
                {
                    if (r is JsonElement je)
                    {
                        return JObject.Parse(je.GetRawText());
                    }

                    return JObject.FromObject(r);
                }).ToList();

                return meta.Update<T>(jobjs, userId, true, ctx);
            });
    }

    public static void Delete<T>(IObjectTypeDescriptor<Mutation> desc, ILogicManager logicManager, ILogger logger)
        where T : BaseObject, new()
    {
        var type = typeof(T);

        desc.Field($"delete{type.Name}")
            .Argument("ids", a => a.Type<NonNullType<ListType<NonNullType<UuidType>>>>())
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var userId = Guid.Parse(ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId).Value);
                var meta = ctx.Service<ILimeMeta>();
                ctx.Service<ILimeMetaAuthorizationService>()
                    .EnsureAuthorized(meta, userId, typeof(T), LimeMetaOperation.Delete);

                var ids = ctx.ArgumentValue<List<Guid>>("ids");
                return meta.Delete<T>(r => ids.Contains(r.Id), userId, true, ctx);
            });
    }
}
