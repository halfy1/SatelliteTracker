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

        // ���������� � �������� ����������
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public int SatellitesInUse { get; set; }

        // ���������� � ���������
        [Required]
        [MaxLength(10)]
        public string SatelliteSystem { get; set; } = null!;
        public int SatelliteId { get; set; }
        public double Elevation { get; set; } // � ��������
        public double Azimuth { get; set; }   // � ��������
        public int? SignalToNoiseRatio { get; set; }
        public bool UsedInFix { get; set; }

        // �������������� ����
        public string SentenceType { get; set; } = string.Empty;
    }
}