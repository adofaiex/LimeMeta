namespace LimeMeta;

using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using FreeSql;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Logics;
using LimeMeta.Models;
using LimeMeta.TypeHandlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Text.Json;

/// <summary>
/// 为项目添加扩展方法的集中定义。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册 LimeMeta 相关服务，并从配置中加载 FreeSql 设置。
    /// </summary>
    /// <param name="services">DI 服务集合。</param>
    /// <param name="configuration">应用配置（支持 YAML）。</param>
    /// <param name="env"></param>
    /// <returns>返回服务集合以便链式调用。</returns>
    public static IServiceCollection AddLimeMeta(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var config = configuration.GetSection("LimeMeta").Get<LimeMetaConfiguration>()!;
        services.Configure<LimeMetaConfiguration>(configuration.GetSection("LimeMeta"));
        services.AddSingleton(config);

        // 添加 FreeSql
        services.AddFreeSql();
        services.AddSingleton<ILogicManager, LogicManager>();
        services.AddScoped<ILimeMeta, FreeSqlLimeMeta>();

        // 添加 Jwt（支持 Authorization: Bearer 与 URL 查询参数 access_token，与 login 返回的 JWT 一致）
        services.AddAuthenticationJwtBearer(s => s.SigningKey = config.JwtSignKey, opt =>
        {
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var auth = context.Request.Headers.Authorization.ToString();
                    if (string.IsNullOrEmpty(auth) ||
                        !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        else
                        {
                            StringValues appKey;

                            if (context.Request.Headers.TryGetValue("x-limemeta-app-key", out appKey) || context.Request.Query.TryGetValue("app_key", out appKey))
                            {
                                using var sp = services.BuildServiceProvider();
                                var meta = sp.GetRequiredService<ILimeMeta>();
                                var obj = meta.Query<AppKey>().Include(r => r.User).FirstOrDefault(r => r.Key == appKey);
                                if (obj?.User != null)
                                {
                                    if (obj.Expired < 0 || obj.Expired >= DateTimeOffset.Now.ToUnixTimeMilliseconds())
                                    {
                                        context.Token = UserLogic.GenerateJwt(config, obj.User);
                                    }
                                }
                            }
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization();
        services.AddFastEndpoints();

        if (env.IsDevelopment())
        {
            services.SwaggerDocument();
        }

        return services;
    }

    /// <summary>
    /// 从 appsettings 中读取 FreeSql 连接信息并注册 <see cref="IFreeSql"/>。
    /// 优先读取 <c>ConnectionStrings:FreeSql</c>，其次 <c>LimeMeta:ConnectionString</c>；
    /// 数据库类型读取 <c>LimeMeta:DataType</c>（默认为 SqlServer）。
    /// </summary>
    /// <param name="services">DI 服务集合。</param>
    /// <returns>返回服务集合以便链式调用。</returns>
    /// <exception cref="InvalidOperationException">未提供连接字符串时抛出。</exception>
    private static IServiceCollection AddFreeSql(this IServiceCollection services)
    {
        FreeSql.Internal.Utils.IsStrict = false;
        FreeSql.Internal.Utils.TypeHandlers.TryAdd(typeof(JsonElement), new JsonElementTypeHandler());
        services.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<LimeMetaConfiguration>();
            var builder = new FreeSqlBuilder()
                .UseConnectionString(cfg.DataType, cfg.ConnectionString!)
                .UseAdoConnectionPool(true)
                .UseAutoSyncStructure(false);

            var logger = sp.GetService<ILogger<FreeSqlBuilder>>();
            if (logger != null)
            {
                builder.UseMonitorCommand(cmd => logger.LogInformation("SQL: {sql}\n", cmd.CommandText)).UseNoneCommandParameter(true);
            }

            var fsql = builder.Build();
            fsql.Aop.ConfigEntity += (s, e) =>
            {
                var tabAttr = e.EntityType.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
                if (tabAttr == null || !e.EntityType.IsSubclassOf(typeof(Models.BaseObject)))
                {
                    return;
                }

                foreach (var pi in e.EntityType.GetProperties())
                {
                    var colAttr = pi.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault() as ColumnAttribute;
                    if (colAttr == null) continue;

                    var idxAttr = pi.GetCustomAttributes(typeof(IndexedAttribute), true).FirstOrDefault() as IndexedAttribute;
                    if (idxAttr != null)
                    {
                        var name = $"ix_{tabAttr.Name.Replace('.', '_')}_{colAttr.Name}";
                        var attr = new IndexAttribute(name, colAttr.Name);
                        if (pi.PropertyType.IsSubclassOf(typeof(Geometry)))
                        {
                            attr.IndexMethod = IndexMethod.SP_GiST;
                        }
                        else if (pi.PropertyType == typeof(JsonElement) || pi.PropertyType == typeof(JsonElement?))
                        {
                            attr.IndexMethod = IndexMethod.GIN;
                        }

                        e.ModifyIndexResult.Add(attr);
                    }
                }
            };

            return fsql;
        });

        return services;
    }

    /// <summary>
    /// 配置 LimeMeta 相关中间件/启动逻辑。
    /// </summary>
    /// <param name="app">应用构建器。</param>
    /// <returns>返回应用构建器以便链式调用。</returns>
    public static IApplicationBuilder UseLimeMeta(this IApplicationBuilder app)
    {
        using var sc = app.ApplicationServices.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();
        var meta = sc.ServiceProvider.GetRequiredService<ILimeMeta>();

        if (config.AutoSyncSchema)
        {
            meta.UpdateSchema();
        }

        if (config.LoadSeedOnStartup)
        {
            meta.LoadSeed();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseFastEndpoints();

        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        if (env.IsDevelopment())
        {
            app.UseSwaggerGen();
        }

        return app;
    }

    /// <summary>
    /// 将 UTC DateTime 转换为可读性更好的长整数格式（yyyyMMddHHmmssfff）。
    /// </summary>
    /// <param name="dateTime">要转换的 UTC 日期时间。</param>
    /// <returns>格式为 yyyyMMddHHmmssfff 的长整数，例如 20240101120000123。</returns>
    /// <example>
    /// <code>
    /// var utcNow = DateTime.UtcNow;
    /// long timestamp = utcNow.ToReadableLong(); // 例如：20240101120000123
    /// </code>
    /// </example>
    public static long ToReadableLong(this DateTime dateTime)
    {
        // 确保使用 UTC 时间
        var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();

        return long.Parse(utcDateTime.ToString("yyyyMMddHHmmssfff"));
    }

    /// <summary>
    /// 将可读性更好的长整数格式（yyyyMMddHHmmssfff 或 yyyyMMddHHmmss）转换回 UTC DateTime。
    /// </summary>
    /// <param name="readableLong">格式为 yyyyMMddHHmmssfff（17位）或 yyyyMMddHHmmss（14位）的长整数，例如 20240101120000123 或 20240101120000。</param>
    /// <returns>对应的 UTC DateTime。</returns>
    /// <exception cref="ArgumentException">当长整数格式不正确时抛出。</exception>
    /// <example>
    /// <code>
    /// long timestamp = 20240101120000123;
    /// DateTime utcDateTime = timestamp.FromReadableLong(); // 2024-01-01 12:00:00.123 UTC
    /// </code>
    /// </example>
    public static DateTime FromReadableLong(this long readableLong)
    {
        var str = readableLong.ToString();
        string format;

        if (str.Length == 17)
        {
            format = "yyyyMMddHHmmssfff";
        }
        else if (str.Length == 14)
        {
            format = "yyyyMMddHHmmss";
        }
        else
        {
            throw new ArgumentException($"长整数格式不正确，应为 14 位数字（yyyyMMddHHmmss）或 17 位数字（yyyyMMddHHmmssfff），实际为 {str.Length} 位。", nameof(readableLong));
        }

        if (!DateTime.TryParseExact(str, format, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var result))
        {
            throw new ArgumentException($"无法解析长整数 {readableLong} 为有效的日期时间。", nameof(readableLong));
        }

        return DateTime.SpecifyKind(result, DateTimeKind.Utc);
    }

    /// <summary>
    /// GetMD5
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string GetMD5(this string s)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(s);
        var hash = md5.ComputeHash(bytes);

        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>
    /// GetMD5
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string GetMD5(this Stream s)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(s);

        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>
    /// GetPropertyValue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T? GetPropertyValue<T>(this object obj, string name)
    {
        var type = obj.GetType();
        var pi = type.GetProperty(name);
        if (pi == null) return default;

        return (T?)pi.GetValue(obj);
    }

    /// <summary>
    /// SetPropertyValue
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SetPropertyValue(this object obj, string name, object value)
    {
        var type = obj.GetType();
        var pi = type.GetProperty(name);
        if (pi == null) return;

        pi.SetValue(obj, value);
    }
}

