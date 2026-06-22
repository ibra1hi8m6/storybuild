using Application.DTOs;

namespace Application.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text);
    }

    public interface IVectorStoreService
    {
        // Predefined collection names for this platform
        public const string CollectionLessons    = "arabic_lessons";
        public const string CollectionStories    = "arabic_stories";
        public const string CollectionActivities = "arabic_activities";

        Task EnsureCollectionAsync(string? collectionName = null);
        Task DeleteBySourceAsync(string sourceFile, string? collectionName = null);
        Task AddChunksAsync(
            List<string> ids,
            List<float[]> embeddings,
            List<string> texts,
            List<Dictionary<string, string>> metadatas,
            string? collectionName = null);
        Task<List<RagSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, string? collectionName = null);
    }

    public interface IDocumentProcessor
    {
        bool CanProcess(string extension);
        Task<List<DocumentChunk>> ProcessAsync(string filePath, DocumentMetadata meta);
    }

    public interface IRagIngestionService
    {
        Task<IngestDocumentResponse> IngestAsync(
            Stream fileStream,
            string fileName,
            IngestDocumentRequest request,
            CancellationToken ct = default);

        Task DeleteAsync(Guid documentId);
    }

    public interface IRagQueryService
    {
        Task<string> GetContextAsync(string query, int topK = 5);
        Task<List<RagSearchResult>> SearchAsync(string query, int topK = 5);
    }

    // ── Internal models ───────────────────────────────────────────────────────────
    public record DocumentChunk(
        string Text,
        int PageNumber,
        string ChunkId);

    public record DocumentMetadata(
        string FileName,
        string? Letter,
        int? Level,
        string? Tags);
}
