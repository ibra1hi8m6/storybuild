namespace Application.DTOs
{
    // ── Writing Correction ─────────────────────────────────────────────────────
    public record WritingCorrectionResponse(
        string ExtractedText,
        string ExpectedSentence,
        double SimilarityScore,
        bool IsAccepted,
        string Message,
        string DisplayMessage,
        string SpokenFeedback,
        List<WritingMistakeDto> Mistakes,
        List<string> Tips);

    public record WritingMistakeDto(
        string Type,
        string Expected,
        string Actual,
        string Description);

    public record WritingAttemptHistoryDto(
        Guid Id,
        Guid LessonPageId,
        string ExpectedSentence,
        string ExtractedText,
        double SimilarityScore,
        bool IsAccepted,
        int AttemptNumber,
        string DisplayMessage,
        List<WritingMistakeDto> Mistakes,
        List<string> Tips,
        string ImageUrl,
        DateTime AttemptedAt);

    // ── Reading / Fluency History ──────────────────────────────────────────────
    public record ReadingAttemptHistoryDto(
        Guid RecordingId,
        Guid PageId,
        string PageType,
        string ExpectedText,
        string ExtractedText,
        double WCPM,
        double AccuracyScore,
        bool IsAccepted,
        int AttemptNumber,
        string DisplayMessage,
        List<string> MispronouncedWords,
        List<string> Tips,
        string AudioUrl,
        DateTime CreatedAt);
}
