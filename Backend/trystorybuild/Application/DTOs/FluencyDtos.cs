namespace Application.DTOs
{
    public record FluencyReportDto(
        Guid ReportId,
        Guid RecordingId,
        string AudioFileUrl,
        double WCPM,
        double AccuracyScore,
        string ExpectedText,
        string ExtractedText,
        List<string> MispronouncedWords,
        bool Passed,
        DateTime CreatedAt,
        string DisplayMessage = "",
        List<string>? Tips = null
    );

    public record FluencyHistoryDto(
        Guid RecordingId,
        Guid PageId,
        string PageType,
        string AudioFileUrl,
        double DurationSeconds,
        double WCPM,
        double AccuracyScore,
        bool Passed,
        DateTime CreatedAt
    );
}
