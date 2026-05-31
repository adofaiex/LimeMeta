using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Configuration;
using LimeMeta.Workflow.Configurations;

namespace LimeMeta.Workflow;

/// <summary>
/// 为 Workflow 功能添加扩展方法的集中定义。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册 LimeMeta Workflow 相关服务。
    /// </summary>
    /// <param name="services">DI 服务集合。</param>
    /// <param name="gqlBuilder">GraphQL 构建器。</param>
    /// <returns>返回服务集合以便链式调用。</returns>
    public static IServiceCollection AddLimeMetaWorkflow(this IServiceCollection services, IRequestExecutorBuilder gqlBuilder, IConfiguration configuration)
    {
        var config = configuration.GetSection("LimeMetaWorkflow").Get<LimeMetaWorkflowConfiguration>()!;
        services.AddSingleton(config);

        gqlBuilder.AddTypeExtension<QueryExtensions>();
        return services;
    }

    /// <summary>
    /// 配置 LimeMeta Workflow 中间件和端点。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <returns>返回应用构建器以便链式调用。</returns>
    public static IEndpointRouteBuilder UseLimeMetaWorkflow(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
