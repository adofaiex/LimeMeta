using System.Net.WebSockets;
using System.Security.Claims;

namespace LimeMeta.WebSockets;

/// <summary>
/// LimeMeta WebSocket 连接信息。
/// </summary>
public sealed class LimeMetaWebSocketConnection
{
    internal LimeMetaWebSocketConnection(string id, WebSocket socket, ClaimsPrincipal user)
    {
        Id = id;
        Socket = socket;
        User = user;
    }

    /// <summary>
    /// 连接 ID。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// WebSocket。
    /// </summary>
    public WebSocket Socket { get; }

    /// <summary>
    /// 当前用户。
    /// </summary>
    public ClaimsPrincipal User { get; }

    internal SemaphoreSlim SendLock { get; } = new(1, 1);
}
