namespace Application.DTOs
{
    // ── Story Generation ───────────────────────────────────────────────────────
    public record GenerateStoryRequest(string ChildName, string Character, string Theme, Guid? StudentId = null);

    public record GenerateStoryResponse(
        Guid Id,
        string Title,
        bool IsApproved,
        List<StoryPageDto> Pages,
        int Source = 0);

    public record StoryPageDto(
        Guid PageId,
        int PageNumber,
        string Sentence,
        string ImageUrl,
        bool IsUnlocked);

    // ── Admin-Uploaded PDF Stories ─────────────────────────────────────────────
    public record UploadedStoryDto(
        Guid Id,
        string Title,
        string CoverImageUrl,
        int PageCount,
        DateTime CreatedAt,
        List<StoryPageDto> Pages);

    // ── Internal AI Output ─────────────────────────────────────────────────────
    public record AiStoryOutput(string Title, List<AiStoryPage> Pages);

    public record AiStoryPage(int PageNumber, string Sentence, string ImagePrompt);

    // ── Content Moderation ─────────────────────────────────────────────────────
    public record JudgeResult(bool IsApproved, string Reason);
}
