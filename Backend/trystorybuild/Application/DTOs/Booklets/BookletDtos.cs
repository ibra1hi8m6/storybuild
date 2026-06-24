namespace Application.DTOs
{
    // ── PDF Lesson Import ──────────────────────────────────────────────────────
    public record ImportBookResponse(
        Guid Id,
        string Title,
        int Level,
        string Letter,
        string LetterName,
        int PageCount);

    public record LessonSummaryDto(
        Guid Id,
        int Level,
        string Letter,
        string LetterName,
        string Title,
        string CoverImageUrl,
        int PageCount,
        bool IsPublished = true,
        string Status = "Published");

    public record LessonPageDto(
        Guid PageId,
        int PageNumber,
        string Sentence,
        string ImageUrl,
        bool IsUnlocked,
        bool IsCoverPage);

    public record LessonDetailResponse(
        Guid Id,
        int Level,
        string Letter,
        string LetterName,
        string Title,
        string CoverImageUrl,
        List<LessonPageDto> Pages);

    // ── Admin Book Management ──────────────────────────────────────────────────
    public record AdminBooksPageDto(
        List<LessonSummaryDto> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);

    public record ManualPageDto(string Sentence);

    public record CreateManualBookRequest(
        string Title,
        string LetterName,
        string Letter,
        int Level,
        List<ManualPageDto> Pages);

    public record UpdatePageSentenceRequest(string Sentence);
}
