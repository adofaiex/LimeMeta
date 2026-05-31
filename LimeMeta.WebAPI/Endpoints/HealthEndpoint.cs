using FastEndpoints;

namespace LimeMeta.WebAPI.Endpoints;

/// <summary>
/// HTTP 健康检查示例。
/// </summary>
public sealed class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    /// <summary>
    /// Configure
    /// </summary>
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
    }

    /// <summary>
    /// HandleAsync
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.OkAsync(new HealthResponse
        {
            Status = "ok",
            Service = "LimeMeta",
            Time = DateTimeOffset.UtcNow
        }, ct);
    }
}

/// <summary>
/// HTTP 健康检查示例响应。
/// </summary>
public sealed class HealthResponse
{
    /// <summary>
    /// 状态。
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// 服务名称。
    /// </summary>
    public required string Service { get; set; }

    /// <summary>
    /// 服务端时间。
    /// </summary>
    public DateTimeOffset Time { get; set; }
}
