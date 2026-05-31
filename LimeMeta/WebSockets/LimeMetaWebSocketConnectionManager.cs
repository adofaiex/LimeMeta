using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using LimeMeta.Logics;

namespace LimeMeta.WebSockets;

/// <summary>
/// LimeMeta WebSocket 连接管理器。
/// </summary>
public sealed class LimeMetaWebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, LimeMetaWebSocketConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _userConnections = new();

    /// <summary>
    /// 所有在线连接。
    /// </summary>
    public IReadOnlyCollection<LimeMetaWebSocketConnection> Connections => _connections.Values.ToArray();

    internal LimeMetaWebSocketConnection Add(WebSocket socket, ClaimsPrincipal user)
    {
        var connection = new LimeMetaWebSocketConnection(Guid.NewGuid().ToString("N"), socket, user);
        _connections[connection.Id] = connection;

        var userId = GetUserId(user);
        if (userId != null)
        {
            var ids = _userConnections.GetOrAdd(userId.Value, _ => new ConcurrentDictionary<string, byte>());
            ids[connection.Id] = 0;
        }

        return connection;
    }

    internal void Remove(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var connection))
        {
            return;
        }

        var userId = GetUserId(connection.User);
        if (userId == null)
        {
            return;
        }

        if (_userConnections.TryGetValue(userId.Value, out var ids))
        {
            ids.TryRemove(connectionId, out _);
            if (ids.IsEmpty)
            {
                _userConnections.TryRemove(userId.Value, out _);
            }
        }
    }

    /// <summary>
    /// 按连接 ID 发送。
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="type"></param>
    /// <param name="data"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task SendToConnectionAsync(string connectionId, string type, object? data = null, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(connectionId, out var connection))
        {
            return Task.CompletedTask;
        }

        return LimeMetaWebSocketContext.SendJsonAsync(connection, CreateResponse(type, data), ct);
    }

    /// <summary>
    /// 按用户 ID 发送。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="type"></param>
    /// <param name="data"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task SendToUserAsync(Guid userId, string type, object? data = null, CancellationToken ct = default)
    {
        if (!_userConnections.TryGetValue(userId, out var ids))
        {
            return Task.CompletedTask;
        }

        var tasks = ids.Keys.Select(id => SendToConnectionAsync(id, type, data, ct));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// 广播消息。
    /// </summary>
    /// <param name="type"></param>
    /// <param name="data"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task BroadcastAsync(string type, object? data = null, CancellationToken ct = default)
    {
        var response = CreateResponse(type, data);
        var tasks = _connections.Values.Select(connection =>
            LimeMetaWebSocketContext.SendJsonAsync(connection, response, ct));

        return Task.WhenAll(tasks);
    }

    private static LimeMetaWebSocketResponse CreateResponse(string type, object? data)
    {
        return new LimeMetaWebSocketResponse
        {
            Type = type,
            Success = true,
            Data = data
        };
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.Claims.FirstOrDefault(r => r.Type == UserLogic.ClaimUserId)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
