namespace Domain.Entities
{
    public class WritingAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LessonPageId { get; set; }
        public Guid? LessonId { get; set; }
        public Guid? StudentId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public string UploadedImagePath { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string ExpectedSentence { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public bool IsAccepted { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public string DisplayMessage { get; set; } = string.Empty;
        public string SpokenFeedback { get; set; } = string.Empty;
        public string MistakesJson { get; set; } = "[]";
        public string TipsJson { get; set; } = "[]";
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        public LessonPage LessonPage { get; set; } = null!;
    }
}
