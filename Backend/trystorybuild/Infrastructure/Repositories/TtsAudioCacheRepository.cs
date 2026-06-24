using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TtsAudioCacheRepository(AppDbContext db) : ITtsAudioCacheRepository
    {
        public Task<TtsAudioCache?> GetByHashAsync(string hash) =>
            db.TtsAudioCaches.FirstOrDefaultAsync(c => c.TextHash == hash);

        public async Task SaveAsync(TtsAudioCache cache)
        {
            db.TtsAudioCaches.Add(cache);
            await db.SaveChangesAsync();
        }

        public async Task UpdateUsageAsync(Guid id)
        {
            var entry = await db.TtsAudioCaches.FindAsync(id);
            if (entry is null) return;
            entry.LastUsedAt = DateTime.UtcNow;
            entry.UsageCount++;
            await db.SaveChangesAsync();
        }
    }
}
