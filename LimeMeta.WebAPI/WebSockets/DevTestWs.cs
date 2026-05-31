using LimeMeta.WebSockets;

namespace LimeMeta.WebAPI.WebSockets;

/// <summary>
/// WebSocket 开发示例。
/// </summary>
[WsController]
public sealed class DevTestWs
{
    /// <summary>
    /// WebSocket 健康检查示例。
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [WsMessage("dev.health")]
    public DevHealthResult Health(LimeMetaWebSocketContext ctx)
    {
        return new DevHealthResult
        {
            Status = "ok",
            ConnectionId = ctx.ConnectionId,
            UserId = ctx.UserId,
            Time = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// WebSocket 健康检查示例响应。
/// </summary>
public sealed class DevHealthResult
{
    /// <summary>
    /// 状态。
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// 连接 ID。
    /// </summary>
    public required string ConnectionId { get; set; }

    /// <summary>
    /// 当前用户 ID。
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 服务端时间。
    /// </summary>
    public DateTimeOffset Time { get; set; }
}
