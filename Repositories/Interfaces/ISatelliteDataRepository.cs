using SatelliteTracker.Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SatelliteTracker.Backend.Repositories.Interfaces
{
    public interface ISatelliteDataRepository
    {
        Task AddSatelliteDataAsync(SatelliteData data);
        Task<IEnumerable<SatelliteData>> GetSatelliteDataAsync(DateTime from, DateTime to, string? system);
    }
}