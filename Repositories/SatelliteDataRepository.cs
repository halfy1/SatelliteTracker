using SatelliteTracker.Backend.Models;
using SatelliteTracker.Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SatelliteTracker.Backend.Repositories
{
    public class SatelliteDataRepository : ISatelliteDataRepository
    {
        private readonly AppDbContext _context;

        public SatelliteDataRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddSatelliteDataAsync(SatelliteData data)
        {
            _context.SatelliteData.Add(data);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SatelliteData>> GetSatelliteDataAsync(DateTime from, DateTime to, string? system)
        {
            var query = _context.SatelliteData.AsQueryable();

            query = query.Where(d => d.Timestamp >= from && d.Timestamp <= to);

            if (!string.IsNullOrEmpty(system))
            {
                query = query.Where(d => d.SatelliteSystem == system);
            }

            return await query.ToListAsync();
        }
    }
}
