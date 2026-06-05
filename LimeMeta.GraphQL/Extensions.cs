using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using HotChocolate.Execution.Configuration;

namespace LimeMeta.GraphQL;

/// <summary>
/// 为 GraphQL 功能添加扩展方法的集中定义。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册 LimeMeta GraphQL 相关服务。
    /// </summary>
    /// <param name="services">DI 服务集合。</param>
    /// <returns>返回服务集合以便链式调用。</returns>
    public static IRequestExecutorBuilder AddLimeMetaGraphQL(this IServiceCollection services)
    {
        // 构建临时服务提供者以获取 ILogicManager
        // 注意：这仅在配置阶段使用，不会影响运行时性能
        return services
            .AddGraphQLServer()
            .AddAuthorization()
            .ModifyOptions(opt => opt.UseXmlDocumentation = true)
            .ModifyCostOptions(opt => opt.EnforceCostLimits = false)
            .AddQueryType<QueryType>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddMutationType<MutationType>()
            .AddSpatialTypes()
            .AddSpatialProjections()
            .AddSpatialFiltering();
    }

    /// <summary>
    /// 配置 LimeMeta GraphQL 中间件和端点。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <returns>返回应用构建器以便链式调用。</returns>
    public static IEndpointRouteBuilder UseLimeMetaGraphQL(this IEndpointRouteBuilder app)
    {
        app.MapGraphQL("/api/gql");
        return app;
    }
}
