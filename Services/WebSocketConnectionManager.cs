using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using SatelliteTracker.Backend.Models;
using Newtonsoft.Json;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public string AddSocket(WebSocket socket)
    {
        string connId = Guid.NewGuid().ToString();
        _sockets.TryAdd(connId, socket);
        return connId;
    }

    public ConcurrentDictionary<string, WebSocket> GetAllSockets()
    {
        return _sockets;
    }

    public void RemoveSocket(string id)
    {
        _sockets.TryRemove(id, out _);
    }

    public async Task BroadcastData(SatelliteData data)
    {
        var sockets = _sockets;
        if (sockets.Count == 0) return;

        var json = JsonConvert.SerializeObject(data);
        var buffer = Encoding.UTF8.GetBytes(json);

        var tasks = new List<Task>();

        foreach (var socket in sockets)
        {
            if (socket.Value.State == WebSocketState.Open)
            {
                var sendTask = socket.Value.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                tasks.Add(sendTask);
            }
        }

        await Task.WhenAll(tasks);
    }

    public async Task SendMessageToClientAsync(string id, string message)
    {
        if (_sockets.TryGetValue(id, out WebSocket socket))
        {
            if (socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
