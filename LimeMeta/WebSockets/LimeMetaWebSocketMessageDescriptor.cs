using System.Reflection;

namespace LimeMeta.WebSockets;

internal sealed class LimeMetaWebSocketMessageDescriptor
{
    public required string Type { get; init; }

    public required Type ControllerType { get; init; }

    public required MethodInfo Method { get; init; }
}
