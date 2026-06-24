using Domain.Entities;

namespace Application.Interfaces
{
    // ── Level Word Config ──────────────────────────────────────────────────────
    public interface ILevelWordConfigRepository
    {
        Task<int> GetWordCountAsync(int level);
        Task UpsertAsync(int level, int wordCount, string exampleSentence);
        Task<List<LevelWordConfig>> GetAllAsync();
    }

    // ── RAG Page Chunks ────────────────────────────────────────────────────────
    public interface IRagPageChunkRepository
    {
        Task<RagPageChunk> SaveAsync(RagPageChunk chunk);
        Task<List<RagPageChunk>> GetAllAsync(int? level = null, string? letter = null);
        Task DeleteBySourceFileAsync(string sourceFile);
    }
}
