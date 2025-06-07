using Microsoft.Extensions.Options;
using SatelliteTracker.Backend.Services.Gps;

public class SimulatedGpsReader : IGpsReader
{
    private readonly GpsSettings _settings;
    private readonly Random _random = new();

    public SimulatedGpsReader(IOptions<GpsSettings> options)
    {
        _settings = options.Value;
    }

    public async Task StartAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Симулируем NMEA-строку с текущими данными
            var simulatedNmea = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47";
            //log
            //Console.WriteLine($"Simulated NMEA: {simulatedNmea}");
            await onMessageReceived(simulatedNmea);

            await Task.Delay(_settings.UpdateIntervalMs, cancellationToken);
        }
    }
}
