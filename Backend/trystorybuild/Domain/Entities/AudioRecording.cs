namespace Domain.Entities
{
    public class AudioRecording
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public Guid PageId { get; set; }
        public string PageType { get; set; } = "Story"; // "Story" | "Lesson"
        public string AudioFileUrl { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public FluencyReport? Report { get; set; }
    }
}
