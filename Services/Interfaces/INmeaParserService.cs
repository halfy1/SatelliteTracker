using SatelliteTracker.Backend.Models;
using System.Threading.Tasks;

namespace SatelliteTracker.Backend.Services.Interfaces
{
    public interface INmeaParserService
    {
        Task<SatelliteData?> ParseNmeaMessage(string nmeaMessage);
    }
}
