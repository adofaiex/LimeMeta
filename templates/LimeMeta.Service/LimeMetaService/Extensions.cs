namespace LimeMetaService;

using HotChocolate.Execution.Configuration;
using LimeMeta.Logics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// LimeMetaService 业务模块入口。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册业务模块服务、配置、GraphQL 扩展和 Logic 程序集。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置。</param>
    /// <param name="gqlBuilder">GraphQL 构建器。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddLimeMetaService(
        this IServiceCollection services,
        IConfiguration configuration,
        IRequestExecutorBuilder gqlBuilder)
    {
        _ = configuration;
        _ = gqlBuilder;

        services.AddLimeMetaModule(typeof(Extensions).Assembly);

        return services;
    }

    /// <summary>
    /// 注册业务模块启动逻辑。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <returns>应用构建器。</returns>
    public static IApplicationBuilder UseLimeMetaService(this IApplicationBuilder app)
    {
        app.UseLimeMetaModule(typeof(Extensions).Assembly);
        return app;
    }
}
