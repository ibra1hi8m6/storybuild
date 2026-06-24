namespace Application.Interfaces
{
    public record TtsAudioResult(string AudioUrl, bool FromCache);

    public interface ITtsService
    {
        Task<TtsAudioResult> GenerateOrGetAudioAsync(string text, string voice = "Kore");
    }
}
