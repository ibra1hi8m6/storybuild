using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.AI
{
    public class GeminiTtsService(
        ITtsAudioCacheRepository cacheRepo,
        CloudinaryService cloudinary,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiTtsService> logger) : ITtsService
    {
        private const string TtsModel        = "gemini-2.5-flash-preview-tts";
        private const string CloudinaryFolder = "lughati/tts";

        public async Task<TtsAudioResult> GenerateOrGetAudioAsync(string text, string voice = "Kore")
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));

            voice = string.IsNullOrWhiteSpace(voice) ? "Kore" : voice;

            var normalized = NormalizeText(text);
            var hash       = ComputeHash(normalized, voice, "gemini");

            var cached = await cacheRepo.GetByHashAsync(hash);
            if (cached is not null)
            {
                logger.LogInformation("[TTS] Cache hit for hash {Hash}", hash[..16]);
                await cacheRepo.UpdateUsageAsync(cached.Id);
                return new TtsAudioResult(cached.AudioUrl, FromCache: true);
            }

            logger.LogInformation("[TTS] Cache miss — calling Gemini TTS for: '{Text}'", text);

            var apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

            var pcmBytes = await CallGeminiTtsAsync(text, voice, apiKey);
            var wavBytes = BuildWav(pcmBytes);

            var shortHash = hash[..16];
            var fileName  = $"tts_{shortHash}.wav";
            var audioUrl  = await cloudinary.UploadRawBytesAsync(wavBytes, fileName, CloudinaryFolder);

            var cacheEntry = new TtsAudioCache
            {
                Text           = text,
                NormalizedText = normalized,
                TextHash       = hash,
                Voice          = voice,
                Provider       = "gemini",
                MimeType       = "audio/wav",
                AudioUrl       = audioUrl,
                PublicId       = $"{CloudinaryFolder}/tts_{shortHash}",
            };

            await cacheRepo.SaveAsync(cacheEntry);
            logger.LogInformation("[TTS] Generated and cached → {Url}", audioUrl);

            return new TtsAudioResult(audioUrl, FromCache: false);
        }

        private async Task<byte[]> CallGeminiTtsAsync(string text, string voice, string apiKey)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{TtsModel}:generateContent?key={apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text } } }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "AUDIO" },
                    speechConfig = new
                    {
                        voiceConfig = new
                        {
                            prebuiltVoiceConfig = new { voiceName = voice }
                        }
                    }
                }
            };

            var client   = httpClientFactory.CreateClient("Gemini");
            var response = await client.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync();
            logger.LogDebug("[TTS] Gemini raw response length: {Len}", raw.Length);

            using var doc = JsonDocument.Parse(raw);

            var inlineData = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("inlineData");

            var base64 = inlineData.GetProperty("data").GetString()
                ?? throw new InvalidOperationException("Gemini TTS returned no audio data.");

            return Convert.FromBase64String(base64);
        }

        private static string NormalizeText(string text)
        {
            var sb = new StringBuilder();
            foreach (var ch in text.Trim())
            {
                if (ch == ' ' && sb.Length > 0 && sb[^1] == ' ') continue;
                sb.Append(ch);
            }
            return sb.ToString().Trim();
        }

        private static string ComputeHash(string normalizedText, string voice, string provider)
        {
            var input = $"{normalizedText}|{voice.ToLowerInvariant()}|{provider.ToLowerInvariant()}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static byte[] BuildWav(byte[] pcm, int sampleRate = 24000, short channels = 1, short bitsPerSample = 16)
        {
            using var ms     = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            var byteRate   = sampleRate * channels * bitsPerSample / 8;
            var blockAlign = (short)(channels * bitsPerSample / 8);
            var dataSize   = pcm.Length;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            writer.Write(pcm);

            return ms.ToArray();
        }
    }
}
