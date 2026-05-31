using FastEndpoints;

namespace LimeMeta.Endpoints;

/// <summary>
/// HealthEndpoint
/// </summary>
public class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
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
/// HealthResponse
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Service
    /// </summary>
    public required string Service { get; set; }

    /// <summary>
    /// Time
    /// </summary>
    public DateTimeOffset Time { get; set; }
}
