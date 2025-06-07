using SatelliteTracker.Backend.Models;
using SatelliteTracker.Backend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MockSatelliteDataRepository : ISatelliteDataRepository
{
    private readonly Random _random = new();

    public Task AddSatelliteDataAsync(SatelliteData data)
    {
        Console.WriteLine($"[MockRepo] Данные спутника добавлены: " +
            $"ID={data.SatelliteId}, System={data.SatelliteSystem}, " +
            $"Time={data.Timestamp:dd.MM.yyyy HH:mm:ss}, " +
            $"Elevation={data.Elevation}°, Azimuth={data.Azimuth}°, " +
            $"SNR={data.SignalToNoiseRatio} dB");

        return Task.CompletedTask;
    }

    public Task<IEnumerable<SatelliteData>> GetSatelliteDataAsync(DateTime from, DateTime to, string? system)
    {
        var systems = new[] { "GPS", "GLONASS", "Galileo", "BeiDou" };
        var result = new List<SatelliteData>();

        for (int i = 0; i < 10; i++)
        {
            var timestamp = from.AddSeconds(_random.Next((int)(to - from).TotalSeconds));
            var sys = systems[_random.Next(systems.Length)];

            if (system == null || sys == system)
            {
                result.Add(new SatelliteData
                {
                    SatelliteId = _random.Next(1, 30),
                    SatelliteSystem = sys,
                    Timestamp = timestamp,
                    Elevation = _random.Next(0, 90),
                    Azimuth = _random.Next(0, 360),
                    SignalToNoiseRatio = _random.Next(20, 60)
                });
            }
        }

        return Task.FromResult((IEnumerable<SatelliteData>)result);
    }
}
