using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LimeMeta.Logics;

/// <summary>
/// 扩展方法
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册 LimeMeta 业务模块程序集，让模型、DTO、Logic 可以被框架明确扫描。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="assembly">业务模块程序集。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddLimeMetaModule(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        services.AddSingleton(new LimeMetaModuleAssembly(assembly));
        return services;
    }

    /// <summary>
    /// 在应用启动阶段注册业务模块程序集中的模型和逻辑。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <param name="assembly">业务模块程序集。</param>
    /// <returns>应用构建器。</returns>
    public static IApplicationBuilder UseLimeMetaModule(this IApplicationBuilder app, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assembly);

        using var scope = app.ApplicationServices.CreateScope();
        var logicManager = scope.ServiceProvider.GetRequiredService<ILogicManager>();
        logicManager.RegisterAssembly(assembly, scope.ServiceProvider);
        return app;
    }
}

/// <summary>
/// LimeMeta 业务模块程序集描述。
/// </summary>
/// <param name="assembly">业务模块程序集。</param>
public sealed class LimeMetaModuleAssembly(Assembly assembly)
{
    /// <summary>
    /// 业务模块程序集。
    /// </summary>
    public Assembly Assembly { get; } = assembly;
}
