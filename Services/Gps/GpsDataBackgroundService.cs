using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SatelliteTracker.Backend.Models;
using SatelliteTracker.Backend.Repositories.Interfaces;
using SatelliteTracker.Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;

namespace SatelliteTracker.Backend.Services
{
    public class GpsDataBackgroundService : BackgroundService
    {
        private readonly ILogger<GpsDataBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly WebSocketConnectionManager _wsManager;

        public GpsDataBackgroundService(
            ILogger<GpsDataBackgroundService> logger,
            IServiceProvider serviceProvider,
            WebSocketConnectionManager wsManager)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _wsManager = wsManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GpsDataBackgroundService started.");

            var gpsReader = _serviceProvider.GetRequiredService<IGpsReader>();

            await gpsReader.StartAsync(async (nmea) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var parser = scope.ServiceProvider.GetRequiredService<INmeaParserService>();
                var repository = scope.ServiceProvider.GetRequiredService<ISatelliteDataRepository>();

                var data = await parser.ParseNmeaMessage(nmea);
                if (data != null)
                {
                    await BroadcastData(data);
                }
            }, stoppingToken);
        }


        private async Task BroadcastData(SatelliteData data)
        {
            var sockets = _wsManager.GetAllSockets();
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
    }
}
