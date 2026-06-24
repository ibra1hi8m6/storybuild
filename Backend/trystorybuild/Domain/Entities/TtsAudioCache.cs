namespace Domain.Entities
{
    public class TtsAudioCache
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string NormalizedText { get; set; } = string.Empty;
        public string TextHash { get; set; } = string.Empty;
        public string Voice { get; set; } = "Kore";
        public string Provider { get; set; } = "gemini";
        public string MimeType { get; set; } = "audio/wav";
        public string AudioUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
        public int UsageCount { get; set; } = 1;
    }
}
