namespace LimeMeta.WebSockets;

/// <summary>
/// 标记 WebSocket 消息处理方法对应的消息类型。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class WsMessageAttribute : Attribute
{
    /// <summary>
    /// WsMessageAttribute
    /// </summary>
    /// <param name="type">消息类型，例如 ping、notice.subscribe。</param>
    public WsMessageAttribute(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("WebSocket 消息类型不能为空。", nameof(type));
        }

        Type = type;
    }

    /// <summary>
    /// 消息类型。
    /// </summary>
    public string Type { get; }
}
