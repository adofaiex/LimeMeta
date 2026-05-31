using System.Text;
using LimeMeta;
using LimeMeta.GraphQL;
using LimeMeta.Workflow;
using Serilog;

Console.OutputEncoding = Encoding.UTF8;
// 确保环境变量被正确读取（在创建 builder 之前）
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = environmentName
});

builder.WebHost.ConfigureKestrel(opt =>
{
    opt.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
});

// 配置 YAML 配置文件支持
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yml", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()  // 环境变量优先级高于 YAML 文件
    .AddCommandLine(args);

// Serilog 从 appsettings 的 Serilog 节点读取配置
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddLimeMeta(builder.Configuration, builder.Environment);

// Add HttpClient for MAF workflow trigger
builder.Services.AddHttpClient("MAF", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var gqlBuilder = builder.Services.AddLimeMetaGraphQL();
builder.Services.AddLimeMetaWorkflow(gqlBuilder, builder.Configuration);

// Build the app
var app = builder.Build();
app.UseLimeMeta();
app.UseLimeMetaGraphQL();
app.UseLimeMetaWorkflow();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

