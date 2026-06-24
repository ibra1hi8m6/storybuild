using Application.DTOs;

namespace Application.Interfaces
{
    // ── Story & Exam Generation ────────────────────────────────────────────────
    public interface IStoryGeneratorService
    {
        Task<AiStoryOutput> GenerateAsync(string childName, string character, string theme);
    }

    public interface IExamGeneratorService
    {
        Task<AiExamOutput> GenerateAsync(string storyText);
        Task<AiExamOutput> GenerateLessonAsync(string lessonText);
    }

    public interface IJudgeService
    {
        Task<JudgeResult> ValidateAsync(string storyTitle, List<string> sentences, List<string> imagePrompts);
    }

    public interface IImageGenerationService
    {
        Task<string> GenerateImageAsync(string prompt, string fileName);
    }

    // ── OCR & Text Processing ──────────────────────────────────────────────────
    public interface IOcrService
    {
        Task<string> ExtractArabicTextAsync(string imagePath);
    }

    public interface ITextSimilarityService
    {
        double Calculate(string expected, string actual);
    }

    public interface IAiTextCleanupService
    {
        Task<string> CleanupArabicSentenceAsync(string ocrText);
    }
}
