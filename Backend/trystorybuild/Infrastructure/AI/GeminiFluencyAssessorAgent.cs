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

            // 5. Compute attempt number for this page
            var pastRecordings = await recordingRepository.GetByPageIdAsync(pageId, studentId);
            var attemptNumber  = pastRecordings.Count; // already saved, so count includes this one

            var isAccepted     = accuracy >= PassThreshold;
            var displayMessage = isAccepted
                ? $"أحسنت! قرأت الجملة بدقة {accuracy:F0}٪"
                : $"حاول مرة أخرى! دقتك {accuracy:F0}٪";
            var tips = new List<string>();
            if (mispronounced.Count > 0 && !isAccepted)
            {
                tips.Add($"ركّز على الكلمات: {string.Join("، ", mispronounced.Take(3))}");
                if (wcpm < 20) tips.Add("حاول القراءة بشكل أبطأ وبوضوح أكبر.");
            }

            // 6. Save report
            var report = await recordingRepository.SaveReportAsync(new FluencyReport
            {
                RecordingId            = recording.Id,
                WCPM                   = wcpm,
                AccuracyScore          = accuracy,
                ExpectedText           = expectedText,
                ExtractedText          = extractedText,
                MispronouncedWordsJson = JsonSerializer.Serialize(mispronounced),
                IsAccepted             = isAccepted,
                AttemptNumber          = attemptNumber,
                DisplayMessage         = displayMessage,
                SpokenFeedback         = displayMessage,
                TipsJson               = JsonSerializer.Serialize(tips)
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
                isAccepted,
                report.CreatedAt,
                displayMessage,
                tips);
        }

        // ── Gemini Files API STT (upload → transcribe → delete) ──────────────────
        private async Task<string> TranscribeAudioAsync(IFormFile audio, string expectedText)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[Fluency] No Gemini ApiKey — returning empty transcription.");
                return string.Empty;
            }

            using var ms = new MemoryStream();
            await audio.CopyToAsync(ms);
            var audioBytes = ms.ToArray();

            var mimeType = audio.ContentType.Contains("ogg") ? "audio/ogg"
                         : audio.ContentType.Contains("mp4") ? "audio/mp4"
                         : "audio/webm";

            var model  = configuration["Gemini:AudioModel"] ?? "gemini-2.0-flash";
            var client = httpClientFactory.CreateClient("Gemini");

            // Step 1: Upload audio to Gemini Files API
            string? fileUri = null;
            string? fileName = null;
            try
            {
                fileUri = await UploadToFilesApiAsync(client, apiKey, audioBytes, mimeType);
                // Extract file name from URI for cleanup: .../files/abc123
                fileName = fileUri?.Split('/').LastOrDefault();
                logger.LogInformation("[Fluency] Uploaded to Files API: {Uri}", fileUri);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Fluency] Files API upload failed — falling back to inline_data.");
            }

            var prompt = $@"You are an Arabic reading evaluator for children aged 3-6.
The child was asked to read: ""{expectedText}""
Transcribe exactly what you hear in the audio. Arabic text only.
Return ONLY valid JSON — no markdown:
{{""transcribedText"": """"}}
If the audio is silent or unclear, return {{""transcribedText"": """"}}.";

            // Build request body — prefer file_data if upload succeeded
            object audioPartObj = fileUri != null
                ? (object)new { file_data = new { file_uri = fileUri, mime_type = mimeType } }
                : new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(audioBytes) } };

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[] { new { text = prompt }, audioPartObj }
                    }
                },
                generationConfig = new { responseMimeType = "application/json" }
            };

            var generateUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            const int maxAttempts = 3;
            string result = string.Empty;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    logger.LogInformation("[Fluency] Gemini STT attempt {A}/{Max}", attempt, maxAttempts);
                    var resp = await client.PostAsJsonAsync(generateUrl, body);
                    resp.EnsureSuccessStatusCode();

                    var raw = await resp.Content.ReadAsStringAsync();
                    using var rootDoc = JsonDocument.Parse(raw);
                    var jsonText = rootDoc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "{}";

                    using var parsed = JsonDocument.Parse(jsonText);
                    var transcribed = parsed.RootElement.TryGetProperty("transcribedText", out var t)
                        ? (t.GetString() ?? string.Empty)
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(transcribed))
                    {
                        logger.LogInformation("[Fluency] STT succeeded on attempt {A}: {T}", attempt, transcribed);
                        result = transcribed;
                        break;
                    }

                    logger.LogWarning("[Fluency] Gemini returned empty text on attempt {A}", attempt);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Fluency] Gemini STT attempt {A} failed.", attempt);
                }

                if (attempt < maxAttempts)
                    await Task.Delay(TimeSpan.FromMilliseconds(800 * attempt));
            }

            // Step 3: Delete uploaded file from Files API (fire-and-forget)
            if (!string.IsNullOrEmpty(fileName))
                _ = DeleteFileAsync(client, apiKey, fileName);

            if (string.IsNullOrEmpty(result))
                logger.LogWarning("[Fluency] All STT attempts failed — returning empty.");

            return result;
        }

        private static async Task<string> UploadToFilesApiAsync(
            HttpClient client, string apiKey, byte[] bytes, string mimeType)
        {
            var uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";

            // Resumable upload: initiate
            var initiateReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            initiateReq.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            initiateReq.Headers.Add("X-Goog-Upload-Command", "start");
            initiateReq.Headers.Add("X-Goog-Upload-Header-Content-Length", bytes.Length.ToString());
            initiateReq.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);
            initiateReq.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { file = new { display_name = "audio_recording" } }),
                System.Text.Encoding.UTF8, "application/json");

            var initiateResp = await client.SendAsync(initiateReq);
            initiateResp.EnsureSuccessStatusCode();

            var uploadSessionUri = initiateResp.Headers.GetValues("X-Goog-Upload-URL").First();

            // Upload bytes
            var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadSessionUri);
            uploadReq.Headers.Add("X-Goog-Upload-Offset", "0");
            uploadReq.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            uploadReq.Content = new ByteArrayContent(bytes);
            uploadReq.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

            var uploadResp = await client.SendAsync(uploadReq);
            uploadResp.EnsureSuccessStatusCode();

            var raw = await uploadResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.GetProperty("file").GetProperty("uri").GetString()
                ?? throw new InvalidOperationException("No file URI in Files API response.");
        }

        private async Task DeleteFileAsync(HttpClient client, string apiKey, string fileName)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/files/{fileName}?key={apiKey}";
                await client.DeleteAsync(url);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[Fluency] Could not delete Files API file {Name}", fileName);
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
