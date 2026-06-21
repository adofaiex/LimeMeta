using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LimeMeta.Models;
using LimeMeta.Logics;
using LimeMeta.Data;
using HotChocolate.Types;
using HotChocolate.Data;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using FreeSql;
using FreeSql.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using HotChocolate.Data.Projections.Context;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Data.Sorting;
using NetTopologySuite.Geometries;
using System.Text.Json;

namespace LimeMeta.GraphQL;
/// <summary>
/// QueryType
/// </summary>
public class QueryType : ObjectType<Query>
{
    public const string WhereName = "where";
    public const string OrderName = "order";

    /// <summary>
    /// LogicManager
    /// </summary>
    /// <value></value>
    public ILogicManager LogicManager { get; }

    /// <summary>
    /// Logger
    /// </summary>
    /// <value></value>
    public ILogger<QueryType> Logger { get; }

    /// <summary>
    /// QueryType
    /// </summary>
    /// <param name="logicManager"></param>
    /// <param name="loggerFactory"></param>
    public QueryType(ILogicManager logicManager, ILoggerFactory loggerFactory)
    {
        LogicManager = logicManager;
        Logger = loggerFactory.CreateLogger<QueryType>();
    }

    /// <summary>
    /// Configure
    /// </summary>
    /// <param name="desc"></param>
    protected override void Configure(IObjectTypeDescriptor<Query> desc)
    {
        desc.Authorize();
        // 动态为每个 ModelType 生成查询字段
        foreach (var modelType in LogicManager.ModelTypes)
        {
            var method = typeof(QueryType)
                .GetMethod(nameof(AddQueryField), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(modelType);

            method.Invoke(null, [desc, Logger]);

        }
    }

    /// <summary>
    /// AddQueryField：支持 where、sort 与导航属性的 FreeSql LINQ 查询。
    /// </summary>
    private static void AddQueryField<T>(IObjectTypeDescriptor<Query> desc, ILogger logger) where T : BaseObject
    {
        var typeName = typeof(T).Name;

        var field = desc.Field(typeName)
            .Argument("page", a => a.Type(typeof(PageModel)))
            .UseProjection()
            .UseFiltering<T>()
            .UseSorting<T>()
            .Resolve(ctx =>
            {
                try
                {
                    var cliam = ctx.GetUser()!.Claims.FirstOrDefault(r => r.Type == UserLogic.ClaimUserId) ?? throw new GraphQLException("用户未认证");
                    var userId = Guid.Parse(cliam.Value);

                    var meta = ctx.Service<ILimeMeta>();
                    var q = meta.Query<T>().AsQueryable();

                    // where
                    var where = Where<T>(ctx);
                    if (where != null)
                    {
                        q = q.Where(where);
                    }

                    // order
                    q = Order(ctx, q);

                    // page
                    var page = new PageModel();
                    var pageArg = ctx.ArgumentOptional<PageModel>("page");
                    if (pageArg.HasValue)
                    {
                        page = pageArg.Value;
                    }

                    // include
                    var includes = new List<IncludeField>();
                    var itemsField = ctx.GetSelectedField().GetFields().FirstOrDefault(r => string.Compare(r.Field.Name, "items", true) == 0);
                    if (itemsField != null)
                    {
                        includes.AddRange(GetIncludeFields(itemsField.GetFields()));
                    }

                    return meta.Select(q.RestoreToSelect(), page, includes, userId, true, ctx);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "查询错误: model={TypeName}, message={Message}", typeName, ex.Message);
                    throw new GraphQLException(ex.Message);
                }
            });

        // aggr
        desc.Field($"{typeName}Aggr")
            .Argument("fields", a => a.Type(typeof(AggrField[])))
            .Argument("groups", a => a.Type<ListType<StringType>>())
            .UseFiltering<T>()
            .Resolve(ctx =>
            {
                var cliam = ctx.GetUser()!.Claims.First(r => r.Type == UserLogic.ClaimUserId);
                var userId = Guid.Parse(cliam.Value);

                var meta = ctx.Service<ILimeMeta>();
                var q = meta.Query<T>();

                var where = Where<T>(ctx);
                if (where != null)
                {
                    q = q.Where(where);
                }

                var fieldsArg = ctx.ArgumentValue<AggrField[]>("fields");
                var groupsArg = ctx.ArgumentOptional<List<string>>("groups");

                var json = meta.Aggr(q, fieldsArg, groupsArg.HasValue ? groupsArg.Value : null, userId, true, ctx);
                return JsonDocument.Parse(json.ToString()).RootElement;
            });
    }

    /// <summary>
    /// GetIncludeFields
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static IEnumerable<IncludeField> GetIncludeFields(IEnumerable<ISelectedField> fields)
    {
        var result = new List<IncludeField>();

        foreach (var field in fields)
        {
            if (field.Type.IsObjectType() || field.Type.IsListType())
            {
                if (field.Type.ToRuntimeType().IsSubclassOf(typeof(Geometry)))
                {
                    continue;
                }

                var item = new IncludeField
                {
                    Name = field.Field.Name,
                    Type = IncludeFieldType.Object
                };

                if (field.Type.IsListType())
                {
                    item.Type = IncludeFieldType.List;
                }

                result.Add(item);

                var child = field.GetFields();
                if (child.Any())
                {
                    item.Childs.AddRange(GetIncludeFields(child));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Where
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Expression<Func<T, bool>>? Where<T>(IResolverContext context)
    {
        var argument = context.Selection.Field.Arguments[WhereName];
        var filter = context.LocalContextData.ContainsKey(QueryableFilterProvider.ContextValueNodeKey) &&
            context.LocalContextData[QueryableFilterProvider.ContextValueNodeKey] is IValueNode node
        ? node : context.ArgumentLiteral<IValueNode>(WhereName);

        var skipFiltering =
            context.LocalContextData.TryGetValue(QueryableFilterProvider.SkipFilteringKey, out var skip) &&
            skip is true;

        context.LocalContextData = context.LocalContextData.SetItem(QueryableFilterProvider.SkipFilteringKey, true);
        if (!(filter.IsNull() || skipFiltering))
        {
            if (argument.Type is FilterInputType filterInput &&
                context.Selection.GetFilterFeature() is { ArgumentVisitor: { } executor })
            {
                var visitorContext = executor(filter, filterInput, false);
                if (visitorContext.TryCreateLambda(out Expression<Func<T, bool>>? where))
                {
                    return where;
                }
                else
                {
                    if (visitorContext.Errors.Count > 0)
                    {
                        foreach (var error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Order
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public static IQueryable<T> Order<T>(IResolverContext context, IQueryable<T> query)
    {
        var argument = context.Selection.Field.Arguments[OrderName];
        var sort = context.ArgumentLiteral<IValueNode>(OrderName);

        var skipSorting =
            context.LocalContextData.TryGetValue(QueryableSortProvider.SkipSortingKey, out var skip) &&
            skip is true;

        context.LocalContextData =
            context.LocalContextData.SetItem(QueryableSortProvider.SkipSortingKey, true);

        if (sort.IsNull() || skipSorting)
        {
            return query;
        }

        if (context.Selection.Field.Features.TryGet(out SortingFeature? sortingFeature) &&
            sortingFeature is not null &&
            argument.Type is ListType lt &&
            lt.ElementType is NonNullType nn &&
            nn.NamedType() is ISortInputType sortInput &&
            sortingFeature.ArgumentVisitor is { } executor)
        {
            var visitorContext = executor(sort, sortInput, false);

            // compile expression tree
            if (visitorContext.Errors.Count > 0)
            {
                foreach (var error in visitorContext.Errors)
                {
                    context.ReportError(error.WithPath(context.Path));
                }
            }
            else
            {
                return SortFreeSql(visitorContext, query);
            }
        }

        return query;
    }

    /// <summary>
    /// SortFreeSql
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="context"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IQueryable<TSource> SortFreeSql<TSource>(QueryableSortContext context, IQueryable<TSource> source)
    {
        if (context.Operations.Count == 0)
        {
            return source;
        }

        var mis = typeof(Queryable).GetMethods();

        var miOrderBy = mis.First(r => r.Name == nameof(Queryable.OrderBy));
        var miOrderByDesc = mis.First(r => r.Name == nameof(Queryable.OrderByDescending));
        var miThenBy = mis.First(r => r.Name == nameof(Queryable.ThenBy));
        var miThenByDesc = mis.First(r => r.Name == nameof(Queryable.ThenByDescending));

        var firstOperation = true;
        foreach (var operation in context.Operations)
        {
            var type = operation.GetType();
            MethodInfo mi;

            if (firstOperation)
            {
                if (type.Name == "AscendingSortOperation")
                {
                    mi = miOrderBy;
                }
                else
                {
                    mi = miOrderByDesc;
                }
            }
            else
            {
                if (type.Name == "AscendingSortOperation")
                {
                    mi = miThenBy;
                }
                else
                {
                    mi = miThenByDesc;
                }
            }

            var exp = Expression.Lambda(operation.Selector, operation.ParameterExpression);
            source = (IQueryable<TSource>)mi.MakeGenericMethod(typeof(TSource), operation.Selector.Type).Invoke(null, new object[] { source, exp })!;

            firstOperation = false;
        }

        return source;
    }
}
