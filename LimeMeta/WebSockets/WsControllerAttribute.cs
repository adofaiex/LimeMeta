namespace LimeMeta.WebSockets;

/// <summary>
/// 标记一个类为 LimeMeta WebSocket 消息控制器。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WsControllerAttribute : Attribute
{
}
