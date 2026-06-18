namespace Application.DTOs
{
    public record AnnotationDto(
        Guid Id,
        Guid StudentId,
        Guid PageId,
        string PageType,
        string Type,
        string ColorHex,
        string SelectedText,
        int StartOffset,
        int EndOffset,
        DateTime CreatedAt
    );

    public record SaveAnnotationRequest(
        Guid StudentId,
        Guid PageId,
        string PageType,
        string Type,
        string ColorHex,
        string SelectedText,
        int StartOffset,
        int EndOffset
    );

    public record WordJournalDto(
        Guid Id,
        string Word,
        string Definition,
        string? ImageUrl,
        DateTime CreatedAt
    );

    public record AddToJournalRequest(
        Guid StudentId,
        string Word,
        string Definition,
        string? ImageUrl
    );

    public record LessonVocabularyDto(
        Guid Id,
        Guid LessonId,
        string Word,
        string Definition,
        string? ImageUrl,
        int Order
    );

    public record AddLessonVocabularyRequest(
        Guid LessonId,
        string Word,
        string Definition,
        string? ImageUrl,
        int Order
    );
}
