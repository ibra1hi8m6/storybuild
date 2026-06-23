using Domain.Entities;

namespace Application.DTOs
{
    // ── Letter ─────────────────────────────────────────────────────────────────
    public class LetterContentDto
    {
        public Guid Id { get; set; }
        public string Letter { get; set; } = string.Empty;
        public string LetterName { get; set; } = string.Empty;
        public string ExampleWord { get; set; } = string.Empty;
        public string DisplaySentence { get; set; } = string.Empty;
        public string AudioText { get; set; } = string.Empty;
        public string WritingTarget { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpsertLetterContentRequest
    {
        public string Letter { get; set; } = string.Empty;
        public string LetterName { get; set; } = string.Empty;
        public string ExampleWord { get; set; } = string.Empty;
        public string DisplaySentence { get; set; } = string.Empty;
        public string AudioText { get; set; } = string.Empty;
        public string WritingTarget { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }

    // ── Word ───────────────────────────────────────────────────────────────────
    public class WordContentDto
    {
        public Guid Id { get; set; }
        public string DisplayWord { get; set; } = string.Empty;
        public string AudioText { get; set; } = string.Empty;
        public string RelatedLetter { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool IsPublished { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpsertWordContentRequest
    {
        public string DisplayWord { get; set; } = string.Empty;
        public string AudioText { get; set; } = string.Empty;
        public string RelatedLetter { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }

    // ── Sentence ───────────────────────────────────────────────────────────────
    public class SentenceContentDto
    {
        public Guid Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Option1 { get; set; } = string.Empty;
        public string Option1Audio { get; set; } = string.Empty;
        public string Option2 { get; set; } = string.Empty;
        public string Option2Audio { get; set; } = string.Empty;
        public string Option3 { get; set; } = string.Empty;
        public string Option3Audio { get; set; } = string.Empty;
        public int CorrectOptionIndex { get; set; }
        public bool IsPublished { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpsertSentenceContentRequest
    {
        public string Option1 { get; set; } = string.Empty;
        public string Option1Audio { get; set; } = string.Empty;
        public string Option2 { get; set; } = string.Empty;
        public string Option2Audio { get; set; } = string.Empty;
        public string Option3 { get; set; } = string.Empty;
        public string Option3Audio { get; set; } = string.Empty;
        public int CorrectOptionIndex { get; set; } = 1;
        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }

    // ── Attempt ────────────────────────────────────────────────────────────────
    public class LearningAttemptDto
    {
        public Guid Id { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public LearningContentType ContentType { get; set; }
        public Guid ContentId { get; set; }
        public LearningAttemptType AttemptType { get; set; }
        public string ExpectedText { get; set; } = string.Empty;
        public string DetectedText { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsCorrect { get; set; }
        public string FeedbackText { get; set; } = string.Empty;
        public string? FeedbackAudio { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveLearningAttemptRequest
    {
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
    }
}
