namespace Application.DTOs
{
    // ── Story Progress ─────────────────────────────────────────────────────────
    public record ProgressResponse(
        Guid StoryId,
        Guid StudentId,
        int CurrentPage,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        bool ExamCompleted);

    // ── Lesson Progress ────────────────────────────────────────────────────────
    public record LessonProgressRequest(
        Guid LessonId,
        Guid StudentId,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        bool ExamCompleted);

    public record MarkPageRequest(
        Guid StudentId,
        Guid LessonId,
        Guid LessonPageId,
        bool WritingSubmitted);

    public record LessonPageProgressResponse(
        List<Guid> CompletedPageIds,
        int CompletedCount,
        int TotalPages);

    public record CurrentLessonResponse(
        Guid? LessonId,
        string? LessonTitle,
        int CurrentPage,
        int TotalPages,
        int Level);
}
