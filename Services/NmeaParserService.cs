using SatelliteTracker.Backend.Models;
using SatelliteTracker.Backend.Services.Interfaces;
using SatelliteTracker.Backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

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
                if (nmeaMessage.StartsWith("$GPGGA"))
                {
                    var data = ParseGPGGA(nmeaMessage);
                    if (data != null)
                        await _repository.AddSatelliteDataAsync(data);
                    return data;
                }
                else if (nmeaMessage.StartsWith("$GPGSV"))
                {
                    var satellites = ParseGPGSV(nmeaMessage);
                    foreach (var sat in satellites)
                    {
                        await _repository.AddSatelliteDataAsync(sat);
                    }
                    return null;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing NMEA message");
                return null;
            }
        }

        private SatelliteData? ParseGPGGA(string message)
        {
            //$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47
            var parts = message.Split(',');

            if (parts.Length < 15)
                return null;

            var timestamp = DateTime.UtcNow;

            var latitude = ParseLatitude(parts[2], parts[3]);
            var longitude = ParseLongitude(parts[4], parts[5]);
            var altitude = double.TryParse(parts[9], out var alt) ? alt : (double?)null;

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

        private List<SatelliteData> ParseGPGSV(string message)
        {
            //$GPGSV,2,1,08,01,40,083,41,02,17,123,39,03,05,235,36,04,10,302,38*70
            var parts = message.Split(',');

            var satelliteCount = (parts.Length - 4) / 4;
            var result = new List<SatelliteData>();

            for (int i = 0; i < satelliteCount; i++)
            {
                var baseIndex = 4 + i * 4;
                if (parts.Length <= baseIndex + 3) break;

                var sat = new SatelliteData
                {
                    Timestamp = DateTime.UtcNow,
                    SatelliteSystem = "GPS",
                    SatelliteId = int.TryParse(parts[baseIndex], out var id) ? id : 0,
                    Elevation = double.TryParse(parts[baseIndex + 1], out var el) ? el : 0,
                    Azimuth = double.TryParse(parts[baseIndex + 2], out var az) ? az : 0,
                    SignalToNoiseRatio = int.TryParse(parts[baseIndex + 3].Split('*')[0], out var snr) ? snr : null,
                    UsedInFix = false
                };

                result.Add(sat);
            }

            return result;
        }

        private double? ParseLatitude(string value, string direction)
        {
            if (double.TryParse(value, out var v) && !string.IsNullOrEmpty(direction))
            {
                var degrees = Math.Floor(v / 100);
                var minutes = v - (degrees * 100);
                var decimalDegrees = degrees + (minutes / 60);
                return direction == "S" ? -decimalDegrees : decimalDegrees;
            }
            return null;
        }

        private double? ParseLongitude(string value, string direction)
        {
            if (double.TryParse(value, out var v) && !string.IsNullOrEmpty(direction))
            {
                var degrees = Math.Floor(v / 100);
                var minutes = v - (degrees * 100);
                var decimalDegrees = degrees + (minutes / 60);
                return direction == "W" ? -decimalDegrees : decimalDegrees;
            }
            return null;
        }
    }
}
