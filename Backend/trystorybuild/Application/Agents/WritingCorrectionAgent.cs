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
                    عدد الكلمات المطلوبة: {{expectedSentence.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length}} كلمة/كلمات

                    ════════════════════════════════════
                    خطوة 1 — عدّ الكلمات (إلزامي قبل أي تقييم):
                    ════════════════════════════════════
                    - امسح الصورة كاملاً وأحصِ كم كلمة مكتوبة فعلاً (حتى لو كانت على أسطر مختلفة).
                    - الكتابة على أسطر متعددة مقبولة تماماً — المساحة صغيرة للأطفال.
                    - إذا كان عدد الكلمات المكتوبة أقل من عدد الكلمات المطلوبة:
                        • كلمة واحدة من أصل 2 أو أكثر  → الحد الأقصى للدرجة = 40
                        • كلمتان من أصل 3 أو أكثر      → الحد الأقصى للدرجة = 65
                        • جميع الكلمات موجودة           → تابع التقييم الكامل

                    ════════════════════════════════════
                    خطوة 2 — فحص النقاط (الحروف ذات النقاط):
                    ════════════════════════════════════
                    الحروف التالية تتطلب نقاطاً — غياب النقطة = خطأ في الحرف:
                    • نقطة واحدة تحت  : ب
                    • نقطتان فوق      : ت، ي، ن (نقطتان تحت للياء)
                    • ثلاث نقاط فوق   : ث، ش
                    • ثلاث نقاط تحت   : ي (في بعض الأشكال)
                    • نقطة فوق        : ذ، ز، ر (الزاي فقط)، ف، ق (نقطتان)، خ، غ، ض، ظ
                    لكل حرف من هذه الحروف في الجملة المطلوبة: تحقق أن نقطته/نقاطه مرسومة بوضوح.
                    حرف بدون نقطته الإلزامية = خطأ واضح، يُخصم منه في الدرجة.

                    ════════════════════════════════════
                    خطوة 3 — معايير التقييم الكامل:
                    ════════════════════════════════════
                    المعيار الأول — مطابقة النص: هل كُتبت جميع كلمات الجملة؟ هل الحروف صحيحة مع نقاطها؟
                    المعيار الثاني — جودة الخط: وضوح الحروف، قابلية القراءة، شكل الحروف العام.
                    ملاحظة: لا تُعاقب الطفل على الكتابة في أسطر متعددة — قيّم كل كلمة في موضعها.

                    ════════════════════════════════════
                    سلّم الدرجات (بعد تطبيق حد الكلمات في خطوة 1):
                    ════════════════════════════════════
                    95–100 : جميع الكلمات موجودة، الحروف والنقاط صحيحة، الخط واضح وجميل
                    85–94  : جميع الكلمات موجودة، الحروف صحيحة، الخط جيد مع ملاحظة بسيطة
                    75–84  : جميع الكلمات موجودة، خطأ بسيط في حرف أو نقطة أو جودة الخط
                    70–74  : جميع الكلمات موجودة لكن أخطاء واضحة في نقاط أو أشكال حروف
                    أقل من 70 : كلمات ناقصة أو أخطاء جسيمة في الحروف

                    ⚠️ لا تُعطِ 95 أو أكثر إلا إذا كانت جميع الكلمات موجودة وجميع النقاط مرسومة والخط واضح.

                    ════════════════════════════════════
                    قواعد الأخطاء:
                    ════════════════════════════════════
                    1. لا تخترع أخطاء — أبلّغ عن خطأ فقط إذا كان واضحاً تماماً.
                    2. النقاط المفقودة على الحروف الإلزامية = خطأ مؤكد، أبلّغ عنه دائماً.
                    3. كلمة مفقودة من الجملة = خطأ مؤكد، أبلّغ عنه دائماً.
                    4. لا تذكر وصل الحروف إلا إذا كان الانفصال يغيّر معنى الكلمة.
                    5. لا تذكر أي حرف أو كلمة غير موجودة في الجملة المطلوبة.
                    6. حقل mistakes للأخطاء المؤكدة فقط — نقطة مفقودة أو كلمة ناقصة أو حرف خاطئ واضح.

                    أمثلة على النصائح المسموح بها في tips:
                    - "تذكر نقطة حرف الباء تحت الحرف."
                    - "حاول كتابة الكلمة الثانية بوضوح أكثر."
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

                    - detectedText  : ما قرأته من الصورة بالعربية (جميع الكلمات التي رأيتها)، أو "" إذا كانت الصورة فارغة
                    - similarity    : رقم من 0 إلى 100 حسب السلّم أعلاه مع تطبيق حد الكلمات إلزامياً
                    - displayMessage: رسالة عربية مشجعة قصيرة مناسبة للأطفال (جملة واحدة، 70 حرفاً كحد أقصى)
                      * النتيجة >= 85 : "أحسنت! ..." أو "رائع! ..."
                      * النتيجة >= 70 : "جيد جداً! ..." أو "كتابتك جيدة، ..."
                      * النتيجة < 70  : "حاول مرة أخرى! ..." مع ذكر ما يحتاج تحسيناً
                    - spokenFeedback: نفس الرسالة بدون رموز تعبيرية، مناسبة للنطق
                    - mistakes      : أخطاء مؤكدة — نقاط مفقودة، كلمات ناقصة، حروف خاطئة واضحة
                      * type        : "wrong_letter" أو "missing_letter" أو "extra_letter" أو "missing_word" أو "missing_dot"
                      * expected    : الحرف/الكلمة/النقطة المتوقعة
                      * actual      : ما كُتب فعلاً (أو "غائب" إذا لم يُكتب)
                      * description : شرح عربي بسيط بجملة واحدة للطفل
                    - tips          : من 0 إلى 3 نصائح قصيرة بالعربية؛ [] إذا كانت النتيجة >= 95
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
