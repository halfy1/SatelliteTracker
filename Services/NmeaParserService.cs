using SatelliteTracker.Backend.Models;
using SatelliteTracker.Backend.Services.Interfaces;
using SatelliteTracker.Backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace SatelliteTracker.Backend.Services
{
    public class NmeaParserService : INmeaParserService
    {
        private readonly ILogger<NmeaParserService> _logger;
        private readonly ISatelliteDataRepository _repository;

        public NmeaParserService(
            ILogger<NmeaParserService> logger,
            ISatelliteDataRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<SatelliteData?> ParseNmeaMessage(string nmeaMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nmeaMessage))
                {
                    _logger.LogWarning("Empty NMEA message received");
                    return null;
                }

                _logger.LogInformation("Received NMEA message: " + nmeaMessage);

                if (nmeaMessage.StartsWith("$GPGGA"))
                {
                    var data = ParseGPGGA(nmeaMessage);
                    if (data != null)
                    {
                        await _repository.AddSatelliteDataAsync(data);
                        _logger.LogDebug($"Parsed GPGGA: Lat={data.Latitude}, Lon={data.Longitude}");
                    }
                    return data;
                }
                else if (nmeaMessage.StartsWith("$GPGSV"))
                {
                    var satellites = ParseGPGSV(nmeaMessage);
                    foreach (var sat in satellites)
                    {
                        await _repository.AddSatelliteDataAsync(sat);
                    }
                    _logger.LogInformation($"Parsed GPGSV with {satellites.Count} satellites");
                    return null;
                }
                else if (nmeaMessage.StartsWith("$GPGLL"))
                {
                    var data = ParseGLL(nmeaMessage);
                    if (data != null)
                    {
                        await _repository.AddSatelliteDataAsync(data);
                        _logger.LogInformation($"Parsed GPGLL: Lat={data.Latitude}, Lon={data.Longitude}");
                    }
                    return null;
                }

                _logger.LogWarning($"Unsupported NMEA message: {nmeaMessage.Split(',')[0]}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while parsing NMEA: {nmeaMessage}");
                return null;
            }
        }

        private SatelliteData? ParseGPGGA(string message)
        {
            var parts = message.Split(',');

            if (parts.Length < 15)
            {
                _logger.LogWarning($"Invalid GPGGA format: {message}");
                return null;
            }

            var latitude = ParseCoordinate(parts[2], parts[3]);
            var longitude = ParseCoordinate(parts[4], parts[5], isLongitude: true);
            var altitude = TryParseDouble(parts[9]);
            var satellitesInUse = TryParseInt(parts[7]);
            var usedInFix = parts[6] != "0";

            if (latitude == null || longitude == null)
                return null;

            return new SatelliteData
            {
                Timestamp = DateTime.UtcNow,
                SentenceType = "GPGGA",
                SatelliteSystem = "GPS",
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitude,
                SatellitesInUse = satellitesInUse,
                UsedInFix = usedInFix
            };
        }

        private List<SatelliteData> ParseGPGSV(string message)
        {
            var parts = message.Split(',');

            if (parts.Length < 4)
            {
                _logger.LogWarning($"Invalid GPGSV format: {message}");
                return new List<SatelliteData>();
            }

            var result = new List<SatelliteData>();
            var timestamp = DateTime.UtcNow;
            int satelliteCount = (parts.Length - 4) / 4;

            for (int i = 0; i < satelliteCount; i++)
            {
                int baseIndex = 4 + i * 4;
                if (parts.Length <= baseIndex + 3) break;

                string snrRaw = parts[baseIndex + 3].Split('*')[0];

                var sat = new SatelliteData
                {
                    Timestamp = timestamp,
                    SentenceType = "GPGSV",
                    SatelliteSystem = "GPS",
                    SatelliteId = TryParseInt(parts[baseIndex]),
                    Elevation = TryParseDouble(parts[baseIndex + 1]),
                    Azimuth = TryParseDouble(parts[baseIndex + 2]),
                    SignalToNoiseRatio = TryParseInt(snrRaw),
                    UsedInFix = false
                };

                result.Add(sat);
            }

            return result;
        }

        private SatelliteData? ParseGLL(string message)
        {
            var parts = message.Split(',');

            if (parts.Length < 7)
            {
                _logger.LogWarning("Invalid GPGLL format: " + message);
                return null;
            }

            var latitude = ParseCoordinate(parts[1], parts[2]);
            var longitude = ParseCoordinate(parts[3], parts[4], isLongitude: true);

            if (latitude == null || longitude == null)
                return null;

            return new SatelliteData
            {
                Timestamp = DateTime.UtcNow,
                SentenceType = "GPGLL",
                SatelliteSystem = "GPS",
                Latitude = latitude,
                Longitude = longitude,
                UsedInFix = parts[6] == "A"
            };
        }

        private double? ParseCoordinate(string value, string direction, bool isLongitude = false)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(direction))
                return null;

            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var coordinate))
                return null;

            var degrees = Math.Floor(coordinate / 100);
            var minutes = coordinate - degrees * 100;
            var decimalDegrees = degrees + minutes / 60;

            if (isLongitude && decimalDegrees > 180 || !isLongitude && decimalDegrees > 90)
                return null;

            return (direction.ToUpper() == "S" || direction.ToUpper() == "W")
                ? -decimalDegrees : decimalDegrees;
        }

        private double? TryParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result : null;
        }

        private int? TryParseInt(string value)
        {
            return int.TryParse(value, out var result) ? result : null;
        }
    }
}
