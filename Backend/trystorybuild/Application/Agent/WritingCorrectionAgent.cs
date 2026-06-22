using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.Agent
{
    public sealed class WritingCorrectionAgent(
        ILessonRepository lessonRepository,
        IWritingAttemptRepository writingAttemptRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IImageStorageService imageStorage,
        ILogger<WritingCorrectionAgent> logger)
    {
        private const double AcceptanceThreshold = 70.0;

        // ── Lesson-based evaluation (existing flow) ───────────────────────────────
        public async Task<WritingCorrectionResponse> EvaluateAsync(
            Guid lessonPageId,
            Guid lessonId,
            string childName,
            IFormFile image)
        {
            logger.LogInformation("[WritingAgent] Evaluating page {PageId} for {Child}", lessonPageId, childName);

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            var lesson = await lessonRepository.GetByIdAsync(lessonId)
                ?? throw new InvalidOperationException($"Lesson {lessonId} not found.");
            var page = lesson.Pages.FirstOrDefault(p => p.Id == lessonPageId)
                ?? throw new InvalidOperationException($"LessonPage {lessonPageId} not found.");
            if (page.IsCoverPage)
                throw new InvalidOperationException("صفحة الغلاف لا تحتاج تمرين كتابة.");

            var expectedSentence = page.Sentence;
            var base64           = Convert.ToBase64String(imageBytes);
            var geminiResult     = await EvaluateWithGeminiAsync(base64, expectedSentence);

            logger.LogInformation(
                "[WritingAgent] Extracted: '{Text}' | Similarity: {Score:F1}%",
                geminiResult.ExtractedText, geminiResult.Similarity);

            var isAccepted = geminiResult.Similarity >= AcceptanceThreshold;

            if (isAccepted)
            {
                var nextPage = lesson.Pages
                    .FirstOrDefault(p => p.PageNumber == page.PageNumber + 1);
                if (nextPage is not null)
                {
                    nextPage.IsUnlocked = true;
                    await lessonRepository.SaveAsync(lesson);
                    logger.LogInformation("[WritingAgent] Page {N} unlocked.", nextPage.PageNumber);
                }
            }

            var imageUrl = string.Empty;
            try
            {
                var fileName = $"{Guid.NewGuid()}.png";
                imageUrl = await imageStorage.UploadImageAsync(imageBytes, fileName, "lughati/writing");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[WritingAgent] Cloudinary upload failed — storing empty path.");
            }

            var attemptNumber = await writingAttemptRepository.CountByPageAsync(lessonPageId, childName) + 1;

            await writingAttemptRepository.SaveAsync(new WritingAttempt
            {
                LessonPageId      = lessonPageId,
                LessonId          = lessonId,
                ChildName         = childName,
                UploadedImagePath = imageUrl,
                ExtractedText     = geminiResult.ExtractedText,
                ExpectedSentence  = expectedSentence,
                SimilarityScore   = geminiResult.Similarity,
                IsAccepted        = isAccepted,
                AttemptNumber     = attemptNumber,
                DisplayMessage    = geminiResult.DisplayMessage,
                SpokenFeedback    = geminiResult.SpokenFeedback,
                MistakesJson      = JsonSerializer.Serialize(geminiResult.Mistakes),
                TipsJson          = JsonSerializer.Serialize(geminiResult.Tips)
            });

            return new WritingCorrectionResponse(
                geminiResult.ExtractedText, expectedSentence, geminiResult.Similarity,
                isAccepted, geminiResult.DisplayMessage,
                geminiResult.DisplayMessage, geminiResult.SpokenFeedback,
                geminiResult.Mistakes, geminiResult.Tips);
        }

        // ── Standalone canvas evaluation ──────────────────────────────────────────
        public async Task<WritingCorrectionResponse> EvaluateDirectAsync(
            string imageBase64,
            string expectedText)
        {
            logger.LogInformation("[WritingAgent] Direct canvas evaluation for: '{Expected}'", expectedText);
            var result     = await EvaluateWithGeminiAsync(imageBase64, expectedText);
            var isAccepted = result.Similarity >= AcceptanceThreshold;
            return new WritingCorrectionResponse(
                result.ExtractedText, expectedText, result.Similarity,
                isAccepted, result.DisplayMessage,
                result.DisplayMessage, result.SpokenFeedback,
                result.Mistakes, result.Tips);
        }

        // ── Gemini 2.5 Flash vision — structured feedback ─────────────────────────
        private async Task<GeminiWritingResult> EvaluateWithGeminiAsync(
            string base64Image, string expectedSentence)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[WritingAgent] Gemini:ApiKey is not configured.");
                return GeminiWritingResult.Fallback(expectedSentence);
            }

            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            try
            {
                var prompt = $$"""
                    You are an Arabic handwriting evaluator for children learning to write Arabic letters and words.

                    The child was asked to write: "{{expectedSentence}}"

                    Analyze the handwriting image carefully and return ONLY valid JSON — no markdown, no extra text:
                    {
                      "detectedText": "",
                      "similarity": 0,
                      "displayMessage": "",
                      "spokenFeedback": "",
                      "mistakes": [
                        { "type": "", "expected": "", "actual": "", "description": "" }
                      ],
                      "tips": []
                    }

                    Rules:
                    - detectedText: exactly what you read from the image in Arabic, or "" if canvas is empty
                    - similarity: integer 0-100 (how closely the writing matches the expected sentence)
                    - displayMessage: short encouraging Arabic message shown on screen (1 sentence, max 60 chars)
                      * if similarity >= 70: start with "أحسنت!" and praise the score
                      * if similarity < 70: start with "حاول مرة أخرى!" and mention what to fix
                    - spokenFeedback: same message but suitable for text-to-speech (no emojis, plain Arabic)
                    - mistakes: list of specific mistakes; empty [] if correct
                      * type: one of "missing_letter", "extra_letter", "wrong_letter", "wrong_diacritic", "spacing", "shape"
                      * expected: what was expected (Arabic)
                      * actual: what was written (Arabic)
                      * description: one-line Arabic explanation for the child
                    - tips: list of 1-3 short Arabic tip strings to help improve; empty [] if score >= 90
                    """;

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new { inline_data = new { mime_type = "image/png", data = base64Image } }
                            }
                        }
                    },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var model    = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url      = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client   = httpClientFactory.CreateClient("Gemini");
                var response = await client.PostAsJsonAsync(url, body);
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadAsStringAsync();
                logger.LogInformation("[WritingAgent] Gemini raw response: {Raw}", raw);

                using var rootDoc = JsonDocument.Parse(raw);
                var jsonText = rootDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                using var resultDoc = JsonDocument.Parse(jsonText);
                var detected = resultDoc.RootElement.TryGetProperty("detectedText", out var d)
                    ? (d.GetString() ?? string.Empty) : string.Empty;
                var sim = resultDoc.RootElement.TryGetProperty("similarity", out var s)
                    ? Math.Clamp(s.GetDouble(), 0, 100) : 0.0;
                var displayMsg = resultDoc.RootElement.TryGetProperty("displayMessage", out var dm)
                    ? (dm.GetString() ?? string.Empty) : string.Empty;
                var spokenFb = resultDoc.RootElement.TryGetProperty("spokenFeedback", out var sf)
                    ? (sf.GetString() ?? string.Empty) : string.Empty;

                var mistakes = new List<WritingMistakeDto>();
                if (resultDoc.RootElement.TryGetProperty("mistakes", out var mArr) && mArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in mArr.EnumerateArray())
                    {
                        mistakes.Add(new WritingMistakeDto(
                            m.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                            m.TryGetProperty("expected", out var e) ? e.GetString() ?? "" : "",
                            m.TryGetProperty("actual", out var a) ? a.GetString() ?? "" : "",
                            m.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""));
                    }
                }

                var tips = new List<string>();
                if (resultDoc.RootElement.TryGetProperty("tips", out var tArr) && tArr.ValueKind == JsonValueKind.Array)
                    foreach (var tip in tArr.EnumerateArray())
                        if (tip.GetString() is string ts) tips.Add(ts);

                if (string.IsNullOrWhiteSpace(displayMsg))
                    displayMsg = sim >= AcceptanceThreshold
                        ? $"أحسنت! كتبت الجملة بدقة {sim:F0}٪"
                        : $"حاول مرة أخرى! حصلت على {sim:F0}٪";
                if (string.IsNullOrWhiteSpace(spokenFb))
                    spokenFb = displayMsg;

                return new GeminiWritingResult(detected, sim, displayMsg, spokenFb, mistakes, tips);
            }
            catch (HttpRequestException ex) when (
                attempt < maxAttempts &&
                ex.StatusCode is System.Net.HttpStatusCode.ServiceUnavailable   // 503
                                or System.Net.HttpStatusCode.TooManyRequests    // 429
                                or System.Net.HttpStatusCode.InternalServerError) // 500
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s
                logger.LogWarning("[WritingAgent] Gemini {Status} on attempt {A}/{Max}. Retrying in {D}s.",
                    ex.StatusCode, attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WritingAgent] Gemini evaluation failed after {A} attempt(s).", attempt);
                return GeminiWritingResult.Fallback(expectedSentence);
            }

            // All retries exhausted
            return GeminiWritingResult.Fallback(expectedSentence);
        }

    }

    // ── Internal result record ────────────────────────────────────────────────────
    internal record GeminiWritingResult(
        string ExtractedText,
        double Similarity,
        string DisplayMessage,
        string SpokenFeedback,
        List<WritingMistakeDto> Mistakes,
        List<string> Tips)
    {
        public static GeminiWritingResult Fallback(string expected) => new(
            string.Empty, 0,
            $"تعذّر تحليل الكتابة. الجملة: {expected}",
            $"تعذّر تحليل الكتابة. الجملة: {expected}",
            [], []);
    }
}
