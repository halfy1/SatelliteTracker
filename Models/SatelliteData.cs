using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatelliteTracker.Backend.Models
{
    public class SatelliteData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        // Координаты и основная информация
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public int? SatellitesInUse { get; set; }

        // Информация о спутниках (для GPGSV)
        [Required]
        [MaxLength(10)]
        public string SatelliteSystem { get; set; } = "GPS";

        public int? SatelliteId { get; set; }
        public double? Elevation { get; set; } // градусы
        public double? Azimuth { get; set; }   // градусы
        public int? SignalToNoiseRatio { get; set; }

        public bool UsedInFix { get; set; }

        // Тип предложения NMEA
        public string SentenceType { get; set; } = string.Empty;

        // Параметры точности (GPGSA)
        public double? PDOP { get; set; }
        public double? HDOP { get; set; }
        public double? VDOP { get; set; }

        // Навигационные параметры (GPRMC)
        public double? Speed { get; set; }       // в узлах
        public double? Direction { get; set; }   // в градусах
    }
}
