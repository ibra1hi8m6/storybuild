namespace Application.DTOs
{
    public record TtsRequest(string Text, string? Voice);
    public record TtsResponse(string AudioUrl, bool FromCache);
}
