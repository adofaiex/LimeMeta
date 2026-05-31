namespace LimeMeta.WebSockets;

/// <summary>
/// LimeMeta WebSocket 配置。
/// </summary>
public sealed class LimeMetaWebSocketOptions
{
    /// <summary>
    /// WebSocket 统一入口路径。
    /// </summary>
    public string Path { get; set; } = "/api/ws";

    /// <summary>
    /// 单条消息最大字节数。
    /// </summary>
    public int MaxMessageSize { get; set; } = 1024 * 1024;
}
