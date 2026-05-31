using System.Text.Json;

namespace LimeMeta.WebSockets;

/// <summary>
/// WebSocket 请求消息。
/// </summary>
public sealed class LimeMetaWebSocketRequest
{
    /// <summary>
    /// 请求 ID。客户端可用它匹配响应。
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 消息类型。
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// 消息数据。
    /// </summary>
    public JsonElement? Data { get; set; }
}

/// <summary>
/// WebSocket 响应消息。
/// </summary>
public sealed class LimeMetaWebSocketResponse
{
    /// <summary>
    /// 请求 ID。
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 响应类型。
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应数据。
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 错误信息。
    /// </summary>
    public string? Error { get; set; }
}
