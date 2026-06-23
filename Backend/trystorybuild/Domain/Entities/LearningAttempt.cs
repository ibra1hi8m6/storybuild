namespace Domain.Entities
{
    public enum LearningContentType
    {
        LetterSound      = 1,
        LetterRecognition = 2,
        WordPractice     = 3,
        SentencePractice = 4,
        Booklet          = 5,
        Story            = 6
    }

    public enum LearningAttemptType
    {
        Writing = 1,
        Reading = 2
    }

    public class LearningAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ChildName { get; set; } = string.Empty;
        public Guid? StudentId { get; set; }
        public LearningContentType ContentType { get; set; }
        public Guid ContentId { get; set; }
        public LearningAttemptType AttemptType { get; set; }
        public string ExpectedText { get; set; } = string.Empty;
        public string DetectedText { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsCorrect { get; set; }
        public string FeedbackText { get; set; } = string.Empty;
        public string? FeedbackAudio { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Student? Student { get; set; }
    }
}
