using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LimeMeta.WebSockets;

/// <summary>
/// LimeMeta WebSocket 扩展。
/// </summary>
public static class LimeMetaWebSocketExtensions
{
    /// <summary>
    /// 注册 LimeMeta WebSocket 服务。
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddLimeMetaWebSockets(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LimeMetaWebSocketOptions>(configuration.GetSection("LimeMeta:WebSocket"));
        return services.AddLimeMetaWebSockets();
    }

    /// <summary>
    /// 注册 LimeMeta WebSocket 服务。
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddLimeMetaWebSockets(this IServiceCollection services)
    {
        services.TryAddSingleton<LimeMetaWebSocketConnectionManager>();
        services.TryAddSingleton<LimeMetaWebSocketDispatcher>();

        foreach (var descriptor in DiscoverDescriptors())
        {
            services.TryAdd(ServiceDescriptor.Transient(descriptor.ControllerType, descriptor.ControllerType));
            services.AddSingleton(descriptor);
        }

        return services;
    }

    /// <summary>
    /// 启用 LimeMeta WebSocket 统一入口。
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseLimeMetaWebSockets(this IApplicationBuilder app)
    {
        app.UseWebSockets();

        app.Use(async (context, next) =>
        {
            var options = context.RequestServices.GetRequiredService<IOptions<LimeMetaWebSocketOptions>>().Value;
            if (!context.Request.Path.Equals(new PathString(options.Path), StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket request required.");
                return;
            }

            var dispatcher = context.RequestServices.GetRequiredService<LimeMetaWebSocketDispatcher>();
            await dispatcher.HandleAsync(context, context.RequestAborted);
        });

        return app;
    }

    private static IEnumerable<LimeMetaWebSocketMessageDescriptor> DiscoverDescriptors()
    {
        var result = new Dictionary<string, LimeMetaWebSocketMessageDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in GetLoadableTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            if (type.GetCustomAttribute<WsControllerAttribute>() == null)
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<WsMessageAttribute>();
                if (attr == null)
                {
                    continue;
                }

                if (result.ContainsKey(attr.Type))
                {
                    throw new InvalidOperationException($"WebSocket 消息类型重复：{attr.Type}");
                }

                var bodyParameterCount = method.GetParameters()
                    .Count(r => r.ParameterType != typeof(LimeMetaWebSocketContext) &&
                                r.ParameterType != typeof(CancellationToken));
                if (bodyParameterCount > 1)
                {
                    throw new InvalidOperationException($"WebSocket 消息方法只能有一个消息体参数：{type.FullName}.{method.Name}");
                }

                result[attr.Type] = new LimeMetaWebSocketMessageDescriptor
                {
                    Type = attr.Type,
                    ControllerType = type,
                    Method = method
                };
            }
        }

        return result.Values;
    }

    private static IEnumerable<Type> GetLoadableTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(r => !r.IsDynamic))
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(r => r != null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                yield return type;
            }
        }
    }
}
