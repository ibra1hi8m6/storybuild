using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITtsAudioCacheRepository
    {
        Task<TtsAudioCache?> GetByHashAsync(string hash);
        Task SaveAsync(TtsAudioCache cache);
        Task UpdateUsageAsync(Guid id);
    }
}
