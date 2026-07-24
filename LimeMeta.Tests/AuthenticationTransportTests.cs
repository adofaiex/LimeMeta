using FreeSql;
using LimeMeta.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LimeMeta.Tests;

public sealed class AuthenticationTransportTests
{
    [Fact]
    public async Task AccessTokenQuery_IsIgnoredForHttpAndAcceptedOnlyOnWebSocketPath()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var options = new JwtBearerOptions();
        var config = new LimeMetaConfiguration
        {
            JwtSignKey = "0123456789abcdef0123456789abcdef"
        };

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
        httpContext.Request.Path = "/api/gql";
        httpContext.Request.QueryString = new QueryString("?access_token=http-token&appkey=query-key");
        var httpMessage = new MessageReceivedContext(
            httpContext,
            new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme,
                null,
                typeof(JwtBearerHandler)),
            options);

        await Extensions.HandleAuthenticationMessage(httpMessage, config, "/api/ws");

        Assert.Null(httpMessage.Token);

        var webSocketContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
        webSocketContext.Request.Path = "/api/ws";
        webSocketContext.Request.QueryString = new QueryString("?access_token=websocket-token");
        var webSocketMessage = new MessageReceivedContext(
            webSocketContext,
            new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme,
                null,
                typeof(JwtBearerHandler)),
            options);

        await Extensions.HandleAuthenticationMessage(webSocketMessage, config, "/api/ws");

        Assert.Equal("websocket-token", webSocketMessage.Token);
    }
}
