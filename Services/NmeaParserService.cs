using SatelliteTracker.Backend.Models;
using SatelliteTracker.Backend.Services.Interfaces;
using SatelliteTracker.Backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Diagnostics.Eventing.Reader;

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
                _logger.LogInformation("GpsDataBackgroundService got the messege: " + nmeaMessage);
                if (nmeaMessage.StartsWith("$GPGGA"))
                {
                    var data = ParseGPGGA(nmeaMessage);
                    if (data != null)
                    {
                        await _repository.AddSatelliteDataAsync(data);
                        _logger.LogDebug($"Parsed GPGGA message: Lat={data.Latitude}, Lon={data.Longitude}");
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
                    _logger.LogInformation($"Parsed GPGSV message: {satellites.Count} satellites");
                    return null;
                }
                else if(nmeaMessage.StartsWith("$GPGLL"))
                {
                    var data = ParseGLL(nmeaMessage);
                    if (data != null)
                    {
                        await _repository.AddSatelliteDataAsync(data);
                        _logger.LogInformation($"Parsed GPGLL message: Lat={data.Latitude}, Lon={data.Longitude}");
                    }
                    return null;
                }

                _logger.LogWarning($"Unsupported NMEA message type: {nmeaMessage.Split(',')[0]}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing NMEA message: {nmeaMessage}");
                return null;
            }
        }

        private SatelliteData? ParseGPGGA(string message)
        {
            //$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47
            var parts = message.Split(',');

            if (parts.Length < 15)
            {
                _logger.LogWarning($"Invalid GPGGA format: expected 15 parts, got {parts.Length}");
                return null;
            }

            try
            {
                var timestamp = DateTime.UtcNow;
                var latitude = ParseCoordinate(parts[2], parts[3]);
                var longitude = ParseCoordinate(parts[4], parts[5], isLongitude: true);
                var altitude = TryParseDouble(parts[9]);

                if (latitude == null || longitude == null)
                {
                    _logger.LogWarning($"Failed to parse coordinates: Lat={parts[2]}{parts[3]}, Lon={parts[4]}{parts[5]}");
                    return null;
                }

                return new SatelliteData
                {
                    Timestamp = timestamp,
                    SatelliteSystem = "GPS",
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = altitude,
                    UsedInFix = parts[6] != "0"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing GPGGA message: {message}");
                return null;
            }
        }

        private List<SatelliteData> ParseGPGSV(string message)
        {
            //$GPGSV,2,1,08,01,40,083,41,02,17,123,39,03,05,235,36,04,10,302,38*70
            var parts = message.Split(',');

            if (parts.Length < 4)
            {
                _logger.LogWarning($"Invalid GPGSV format: too short message");
                return new List<SatelliteData>();
            }

            var result = new List<SatelliteData>();
            var satelliteCount = (parts.Length - 4) / 4;

            for (int i = 0; i < satelliteCount; i++)
            {
                var baseIndex = 4 + i * 4;
                if (parts.Length <= baseIndex + 3) break;

                // Remove checksum if present in SNR field
                var snrPart = parts[baseIndex + 3].Split('*')[0];

                var sat = new SatelliteData
                {
                    Timestamp = DateTime.UtcNow,
                    SatelliteSystem = "GPS",
                    SatelliteId = TryParseInt(parts[baseIndex]) ?? 0, // Если null, подставляем 0
                    Elevation = TryParseDouble(parts[baseIndex + 1]) ?? 0,
                    Azimuth = TryParseDouble(parts[baseIndex + 2]) ?? 0,
                    SignalToNoiseRatio = TryParseInt(snrPart) ?? 0, // Если null, подставляем 0
                    UsedInFix = false
                };

                result.Add(sat);
            }

            return result;
        }
        private SatelliteData? ParseGLL(string messege)
        {
            var parts = messege.Split(',');
            if (parts.Length != 8)
            {
                _logger.LogWarning("Invalid GLL messege");
            }

            var result = new SatelliteData();
            result.SatelliteSystem = parts[0];
            result.Latitude = ParseCoordinate(parts[1], parts[2]);
            result.Longitude = ParseCoordinate(parts[3], parts[4], true);
            var ts = new TimeSpan(TryParseInt(parts[5]) ?? 0, (TryParseInt(parts[5]) ?? 0 / 100) % 100, (TryParseInt(parts[5]) ?? 0) % 10000);
            result.Timestamp = DateTime.UtcNow.Date + ts;

            return result;
        }
        private double? ParseCoordinate(string value, string direction, bool isLongitude = false)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(direction))
                return null;

            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var coordinate))
                return null;

            var degrees = Math.Floor(coordinate / 100);
            var minutes = coordinate - (degrees * 100);
            var decimalDegrees = degrees + (minutes / 60);

            // Validate ranges
            if (isLongitude)
            {
                if (decimalDegrees > 180) return null;
                return direction.ToUpper() == "W" ? -decimalDegrees : decimalDegrees;
            }
            else
            {
                if (decimalDegrees > 90) return null;
                return direction.ToUpper() == "S" ? -decimalDegrees : decimalDegrees;
            }
        }

        private double? TryParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }

        private int? TryParseInt(string value)
        {
            if (int.TryParse(value, out var result))
                return result;
            return null;
        }
    }
}