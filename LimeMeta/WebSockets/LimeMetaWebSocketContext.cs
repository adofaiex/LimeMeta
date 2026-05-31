using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LimeMeta.Logics;
using Microsoft.AspNetCore.Http;

namespace LimeMeta.WebSockets;

/// <summary>
/// LimeMeta WebSocket 当前消息上下文。
/// </summary>
public sealed class LimeMetaWebSocketContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal LimeMetaWebSocketContext(
        HttpContext httpContext,
        LimeMetaWebSocketConnection connection,
        LimeMetaWebSocketConnectionManager connections)
    {
        HttpContext = httpContext;
        Connection = connection;
        Connections = connections;
    }

    /// <summary>
    /// HTTP 上下文。
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// 当前连接。
    /// </summary>
    public LimeMetaWebSocketConnection Connection { get; }

    /// <summary>
    /// 连接管理器。
    /// </summary>
    public LimeMetaWebSocketConnectionManager Connections { get; }

    /// <summary>
    /// 连接 ID。
    /// </summary>
    public string ConnectionId => Connection.Id;

    /// <summary>
    /// 当前用户。
    /// </summary>
    public ClaimsPrincipal User => Connection.User;

    /// <summary>
    /// 当前用户 ID。
    /// </summary>
    public Guid? UserId
    {
        get
        {
            var value = User.Claims.FirstOrDefault(r => r.Type == UserLogic.ClaimUserId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    /// <summary>
    /// 发送消息给当前连接。
    /// </summary>
    /// <param name="type">消息类型。</param>
    /// <param name="data">消息数据。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns></returns>
    public Task SendAsync(string type, object? data = null, CancellationToken ct = default)
    {
        var response = new LimeMetaWebSocketResponse
        {
            Type = type,
            Success = true,
            Data = data
        };

        return SendRawAsync(response, ct);
    }

    internal Task SendRawAsync(LimeMetaWebSocketResponse response, CancellationToken ct = default)
    {
        return SendJsonAsync(Connection, response, ct);
    }

    internal static async Task SendJsonAsync(
        LimeMetaWebSocketConnection connection,
        object value,
        CancellationToken ct = default)
    {
        if (connection.Socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(value, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await connection.SendLock.WaitAsync(ct);
        try
        {
            await connection.Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                ct);
        }
        finally
        {
            connection.SendLock.Release();
        }
    }
}
