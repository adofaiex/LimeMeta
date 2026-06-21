using System.Text;
using LimeMeta;
using LimeMeta.GraphQL;
using LimeMetaService;
using Serilog;

Console.OutputEncoding = Encoding.UTF8;

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

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yml", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.WebHost.UseUrls(builder.Configuration.GetValue<string>("Urls") ?? "http://127.0.0.1:6675");

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddLimeMeta(builder.Configuration, builder.Environment);
var gqlBuilder = builder.Services.AddLimeMetaGraphQL();
builder.Services.AddLimeMetaService(builder.Configuration, gqlBuilder);

var app = builder.Build();
app.UseLimeMeta();
app.UseLimeMetaService();
app.UseLimeMetaGraphQL();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
