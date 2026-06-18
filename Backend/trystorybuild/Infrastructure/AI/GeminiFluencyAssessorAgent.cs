using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.AI
{
    public class GeminiFluencyAssessorAgent(
        IAudioStorageService audioStorage,
        IAudioRecordingRepository recordingRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiFluencyAssessorAgent> logger) : IFluencyAssessorAgent
    {
        private const double PassThreshold = 70.0;

        public async Task<FluencyReportDto> EvaluateReadingAsync(
            Guid studentId, Guid pageId, string pageType,
            IFormFile audio, string expectedText)
        {
            logger.LogInformation("[Fluency] Evaluating student {S} on page {P}", studentId, pageId);

            // 1. Upload audio to Cloudinary
            var audioUrl = await audioStorage.UploadAudioAsync(audio);

            // 2. Save recording
            var recording = await recordingRepository.SaveAsync(new AudioRecording
            {
                StudentId       = studentId,
                PageId          = pageId,
                PageType        = pageType,
                AudioFileUrl    = audioUrl,
                DurationSeconds = EstimateDuration(audio.Length)
            });

            // 3. Transcribe with Gemini 2.5 Flash
            var extractedText = await TranscribeAudioAsync(audio, expectedText);
            logger.LogInformation("[Fluency] Extracted: {T}", extractedText);

            // 4. Calculate metrics
            var (accuracy, wcpm, mispronounced) =
                ComputeMetrics(expectedText, extractedText, recording.DurationSeconds);

            // 5. Save report
            var report = await recordingRepository.SaveReportAsync(new FluencyReport
            {
                RecordingId          = recording.Id,
                WCPM                 = wcpm,
                AccuracyScore        = accuracy,
                ExpectedText         = expectedText,
                ExtractedText        = extractedText,
                MispronouncedWordsJson = JsonSerializer.Serialize(mispronounced)
            });

            return new FluencyReportDto(
                report.Id,
                recording.Id,
                audioUrl,
                wcpm,
                accuracy,
                expectedText,
                extractedText,
                mispronounced,
                accuracy >= PassThreshold,
                report.CreatedAt);
        }

        // ── Gemini 2.5 Flash STT ──────────────────────────────────────────────────
        private async Task<string> TranscribeAudioAsync(IFormFile audio, string expectedText)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[Fluency] No Gemini ApiKey — returning empty transcription.");
                return string.Empty;
            }

            try
            {
                using var ms = new MemoryStream();
                await audio.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                var mimeType = audio.ContentType.Contains("ogg") ? "audio/ogg"
                             : audio.ContentType.Contains("mp4") ? "audio/mp4"
                             : "audio/webm";

                var prompt = $@"You are an Arabic reading evaluator for children aged 3-6.
The child was asked to read: ""{expectedText}""
Transcribe exactly what you hear in the audio.
Return ONLY valid JSON — no markdown:
{{""transcribedText"": """"}}
If the audio is silent or unclear, return {{""transcribedText"": """"}}.";

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new { inline_data = new { mime_type = mimeType, data = base64 } }
                            }
                        }
                    },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var model  = configuration["Gemini:AudioModel"] ?? "gemini-2.5-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");
                var resp   = await client.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var raw = await resp.Content.ReadAsStringAsync();
                using var rootDoc = JsonDocument.Parse(raw);
                var jsonText = rootDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                using var result = JsonDocument.Parse(jsonText);
                return result.RootElement.TryGetProperty("transcribedText", out var t)
                    ? (t.GetString() ?? string.Empty)
                    : string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Fluency] Gemini STT failed.");
                return string.Empty;
            }
        }

        // ── Metrics ───────────────────────────────────────────────────────────────
        private static (double accuracy, double wcpm, List<string> mispronounced)
            ComputeMetrics(string expected, string extracted, double durationSeconds)
        {
            var expectedWords  = SplitArabicWords(expected);
            var extractedWords = SplitArabicWords(extracted);
            var extractedSet   = new HashSet<string>(extractedWords, StringComparer.OrdinalIgnoreCase);

            var correctWords   = expectedWords.Count(w => extractedSet.Contains(NormalizeArabic(w)));
            var mispronounced  = expectedWords
                .Where(w => !extractedSet.Contains(NormalizeArabic(w)))
                .Distinct()
                .ToList();

            double accuracy = expectedWords.Count == 0 ? 0
                : Math.Round((double)correctWords / expectedWords.Count * 100, 1);

            double minutes = durationSeconds > 0 ? durationSeconds / 60.0 : 1.0;
            double wcpm    = Math.Round(correctWords / minutes, 1);

            return (accuracy, wcpm, mispronounced);
        }

        private static List<string> SplitArabicWords(string text) =>
            string.IsNullOrWhiteSpace(text)
                ? []
                : text.Split([' ', '\n', '\r', '.', '،', '؟', '!', '"'],
                      StringSplitOptions.RemoveEmptyEntries)
                  .Select(NormalizeArabic)
                  .Where(w => w.Length > 0)
                  .ToList();

        private static string NormalizeArabic(string word) =>
            new string(word
                .Where(c => !((c >= 'ً' && c <= 'ٟ') || c == 'ـ'))
                .ToArray())
            .Trim();

        private static double EstimateDuration(long fileSizeBytes)
        {
            // Rough estimate: webm at ~24kbps ≈ 3000 bytes/sec
            return Math.Max(1, fileSizeBytes / 3000.0);
        }
    }
}
