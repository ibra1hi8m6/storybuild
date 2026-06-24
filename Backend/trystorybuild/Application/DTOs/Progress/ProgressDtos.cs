namespace Application.DTOs
{
    // ── Story Progress ─────────────────────────────────────────────────────────
    public record ProgressResponse(
        Guid StoryId,
        string ChildName,
        int CurrentPage,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        bool ExamCompleted);

    // ── Lesson Progress ────────────────────────────────────────────────────────
    public record LessonProgressRequest(
        Guid LessonId,
        string ChildName,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        bool ExamCompleted);

    public record MarkPageRequest(
        string ChildName,
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
