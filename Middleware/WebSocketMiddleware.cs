using System.Net.WebSockets;
using SatelliteTracker.Backend.Services;
using Microsoft.AspNetCore.Http;
using System.Buffers;

namespace SatelliteTracker.Backend.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketConnectionManager _manager;
        private readonly ILogger<WebSocketMiddleware> _logger;

        public WebSocketMiddleware(
            RequestDelegate next,
            WebSocketConnectionManager manager,
            ILogger<WebSocketMiddleware> logger)
        {
            _next = next;
            _manager = manager;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    string connId = _manager.AddSocket(webSocket);
                    _logger.LogInformation("WebSocket connection established: {ConnectionId}", connId);

                    await HandleWebSocketConnection(webSocket, connId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in WebSocket connection");
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    }
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleWebSocketConnection(WebSocket webSocket, string connectionId)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseConnectionAsync(webSocket, connectionId, result);
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                _manager.RemoveSocket(connectionId);
            }
        }

        private async Task CloseConnectionAsync(WebSocket webSocket, string connectionId, WebSocketReceiveResult closeResult)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(
                        closeResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        closeResult.CloseStatusDescription,
                        CancellationToken.None);
                }
                _logger.LogInformation("WebSocket connection closed: {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while closing WebSocket connection: {ConnectionId}", connectionId);
            }
        }
    }
}