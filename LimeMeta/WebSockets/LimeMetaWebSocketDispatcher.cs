using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimeMeta.WebSockets;

internal sealed class LimeMetaWebSocketDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IReadOnlyDictionary<string, LimeMetaWebSocketMessageDescriptor> _descriptors;
    private readonly IServiceProvider _services;
    private readonly LimeMetaWebSocketConnectionManager _connections;
    private readonly LimeMetaWebSocketOptions _options;
    private readonly ILogger<LimeMetaWebSocketDispatcher> _logger;

    public LimeMetaWebSocketDispatcher(
        IEnumerable<LimeMetaWebSocketMessageDescriptor> descriptors,
        IServiceProvider services,
        LimeMetaWebSocketConnectionManager connections,
        IOptions<LimeMetaWebSocketOptions> options,
        ILogger<LimeMetaWebSocketDispatcher> logger)
    {
        _descriptors = descriptors.ToDictionary(r => r.Type, StringComparer.OrdinalIgnoreCase);
        _services = services;
        _connections = connections;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext httpContext, CancellationToken ct)
    {
        using var socket = await httpContext.WebSockets.AcceptWebSocketAsync();
        var connection = _connections.Add(socket, httpContext.User);
        var wsContext = new LimeMetaWebSocketContext(httpContext, connection, _connections);

        try
        {
            await ReceiveLoopAsync(wsContext, ct);
        }
        finally
        {
            _connections.Remove(connection.Id);
        }
    }

    private async Task ReceiveLoopAsync(LimeMetaWebSocketContext wsContext, CancellationToken ct)
    {
        var buffer = new byte[8192];

        while (!ct.IsCancellationRequested && wsContext.Connection.Socket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await wsContext.Connection.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await wsContext.Connection.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "closed",
                        ct);
                    return;
                }

                ms.Write(buffer, 0, result.Count);

                if (ms.Length > _options.MaxMessageSize)
                {
                    await wsContext.SendRawAsync(CreateError(null, "error", "消息过大。"), ct);
                    return;
                }
            }
            while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(ms.ToArray());
            await DispatchAsync(wsContext, json, ct);
        }
    }

    private async Task DispatchAsync(LimeMetaWebSocketContext wsContext, string json, CancellationToken ct)
    {
        LimeMetaWebSocketRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<LimeMetaWebSocketRequest>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebSocket 消息 JSON 解析失败。");
            await wsContext.SendRawAsync(CreateError(null, "error", "消息格式不正确。"), ct);
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Type))
        {
            await wsContext.SendRawAsync(CreateError(request?.Id, "error", "消息类型不能为空。"), ct);
            return;
        }

        if (!_descriptors.TryGetValue(request.Type, out var descriptor))
        {
            await wsContext.SendRawAsync(CreateError(request.Id, request.Type, $"未找到 WebSocket 消息处理器：{request.Type}。"), ct);
            return;
        }

        try
        {
            var controller = _services.GetRequiredService(descriptor.ControllerType);
            var args = BuildArguments(descriptor.Method, wsContext, request, ct);
            var result = descriptor.Method.Invoke(controller, args);
            var data = await UnwrapResultAsync(result);

            await wsContext.SendRawAsync(new LimeMetaWebSocketResponse
            {
                Id = request.Id,
                Type = $"{request.Type}.result",
                Success = true,
                Data = data
            }, ct);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            _logger.LogError(ex.InnerException, "WebSocket 消息处理失败: {Type}", request.Type);
            await wsContext.SendRawAsync(CreateError(request.Id, request.Type, ex.InnerException.Message), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 消息处理失败: {Type}", request.Type);
            await wsContext.SendRawAsync(CreateError(request.Id, request.Type, ex.Message), ct);
        }
    }

    private static object?[] BuildArguments(
        MethodInfo method,
        LimeMetaWebSocketContext wsContext,
        LimeMetaWebSocketRequest request,
        CancellationToken ct)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.ParameterType == typeof(LimeMetaWebSocketContext))
            {
                args[i] = wsContext;
            }
            else if (parameter.ParameterType == typeof(CancellationToken))
            {
                args[i] = ct;
            }
            else
            {
                args[i] = DeserializeData(request.Data, parameter.ParameterType);
            }
        }

        return args;
    }

    private static object? DeserializeData(JsonElement? data, Type parameterType)
    {
        if (parameterType == typeof(JsonElement) || parameterType == typeof(JsonElement?))
        {
            return data;
        }

        if (data == null || data.Value.ValueKind == JsonValueKind.Null || data.Value.ValueKind == JsonValueKind.Undefined)
        {
            return parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
        }

        return data.Value.Deserialize(parameterType, JsonOptions);
    }

    private static async Task<object?> UnwrapResultAsync(object? result)
    {
        if (result is not Task task)
        {
            return result;
        }

        await task.ConfigureAwait(false);
        var type = task.GetType();
        return type.IsGenericType ? type.GetProperty(nameof(Task<object>.Result))?.GetValue(task) : null;
    }

    private static LimeMetaWebSocketResponse CreateError(string? id, string type, string error)
    {
        return new LimeMetaWebSocketResponse
        {
            Id = id,
            Type = $"{type}.error",
            Success = false,
            Error = error
        };
    }
}
