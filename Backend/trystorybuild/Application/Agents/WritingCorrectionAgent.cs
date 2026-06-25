using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.Agents
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
            Guid studentId,
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

            var attemptNumber = await writingAttemptRepository.CountByPageAndStudentAsync(lessonPageId, studentId) + 1;

            await writingAttemptRepository.SaveAsync(new WritingAttempt
            {
                LessonPageId      = lessonPageId,
                LessonId          = lessonId,
                StudentId         = studentId,
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
                    أنت مقيّم متخصص لخط اليد العربي للأطفال الصغار.
                    مهمتك تقييم الكتابة بدقة وعدالة: لا تبالغ في الثناء، ولا تخترع أخطاء.

                    الجملة المطلوبة: "{{expectedSentence}}"

                    ════════════════════════════════════
                    معايير التقييم — قيّم على أساس المعيارين معاً:
                    ════════════════════════════════════

                    المعيار الأول — مطابقة النص (هل كُتبت الجملة الصحيحة؟)
                    المعيار الثاني — جودة الخط (هل الحروف واضحة ومنظمة؟)
                      • وضوح الحروف وشكلها
                      • المسافات بين الكلمات
                      • انتظام الكتابة على السطر
                      • قابلية القراءة لطفل صغير

                    ════════════════════════════════════
                    سلّم الدرجات:
                    ════════════════════════════════════
                    95–100 : النص صحيح تماماً والخط واضح جداً ومنظم — ممتاز
                    85–94  : النص صحيح والخط جيد جداً مع ملاحظة بسيطة
                    75–84  : النص صحيح أو قريب جداً، لكن جودة الخط تحتاج تحسيناً
                    60–74  : النص مقروء جزئياً أو الخط غير منظم — يحتاج إعادة محاولة
                    أقل من 60 : النص غير واضح أو غير مطابق

                    ⚠️ قاعدة مهمة: إذا كان النص صحيحاً لكن الخط متوسط أو مزدحم، أعطِ 75–88 وليس 100.
                    لا تُعطِ 95 أو أكثر إلا إذا كان الخط واضحاً وجميلاً فعلاً.

                    ════════════════════════════════════
                    قواعد الأخطاء:
                    ════════════════════════════════════
                    1. لا تخترع أخطاء — أبلّغ عن خطأ في حرف فقط إذا كان واضحاً تماماً في الصورة.
                    2. لا تذكر نقاط الحروف (ب، ت، خ، ش...) إلا إذا كانت غائبة تماماً عن الصورة.
                    3. لا تذكر وصل الحروف إلا إذا كان الانفصال واضحاً جداً ويغيّر معنى الكلمة.
                    4. لا تذكر أي حرف أو كلمة غير موجودة في الجملة المطلوبة.
                    5. إذا لم تكن متأكداً من خطأ في حرف معين، استخدم ملاحظة عامة عن جودة الخط بدلاً منه.
                    6. حقل mistakes يجب أن يكون [] في معظم الحالات — لا تملأه إلا عند وجود خطأ مؤكد.

                    أمثلة على الملاحظات العامة المسموح بها في tips:
                    - "حاول أن تجعل الحروف أوضح."
                    - "اترك مسافة صغيرة بين الكلمتين."
                    - "اكتب الحروف على السطر بهدوء."
                    - "الكلمة صحيحة، لكن الخط يحتاج وضوحاً أكثر."
                    - "حاول أن تجعل حجم الحروف متقارباً."

                    ════════════════════════════════════
                    تعليمات JSON — أعد فقط JSON صحيح بدون markdown:
                    ════════════════════════════════════
                    {
                      "detectedText": "",
                      "similarity": 0,
                      "displayMessage": "",
                      "spokenFeedback": "",
                      "mistakes": [],
                      "tips": []
                    }

                    - detectedText  : ما قرأته من الصورة بالعربية، أو "" إذا كانت الصورة فارغة
                    - similarity    : رقم من 0 إلى 100 يجمع بين صحة النص وجودة الخط حسب السلّم أعلاه
                    - displayMessage: رسالة عربية مشجعة قصيرة مناسبة للأطفال (جملة واحدة، 70 حرفاً كحد أقصى)
                      * النتيجة >= 85 : "أحسنت! ..." أو "رائع! ..."
                      * النتيجة >= 70 : "جيد جداً! ..." أو "كتابتك جيدة، ..."
                      * النتيجة < 70  : "حاول مرة أخرى! ..." مع ذكر ما يحتاج تحسيناً
                    - spokenFeedback: نفس الرسالة بدون رموز تعبيرية، مناسبة للنطق
                    - mistakes      : أخطاء مؤكدة وواضحة جداً فقط — [] في معظم الحالات
                      * type        : "wrong_letter" أو "missing_letter" أو "extra_letter" فقط
                      * expected    : الحرف/الكلمة المتوقعة
                      * actual      : ما كُتب فعلاً
                      * description : شرح عربي بسيط بجملة واحدة للطفل
                    - tips          : من 0 إلى 3 نصائح قصيرة بالعربية لتحسين الخط؛ [] إذا كانت النتيجة >= 95
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
        public static GeminiWritingResult Fallback(string expected)
        {
            var label = expected.Trim().Length == 1        ? "الحرف"
                      : !expected.Trim().Contains(' ')     ? "الكلمة"
                      : "الجملة";
            var msg = $"تعذّر تحليل الكتابة. {label}: {expected}";
            return new(string.Empty, 0, msg, msg, [], []);
        }
    }
}
