namespace Domain.Entities
{
    public class FluencyReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecordingId { get; set; }
        public AudioRecording Recording { get; set; } = null!;

        public double WCPM { get; set; }
        public double AccuracyScore { get; set; }
        public string ExpectedText { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string MispronouncedWordsJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
