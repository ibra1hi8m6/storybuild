using Application.Agents;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/writing")]
    public class WritingController(
        WritingCorrectionAgent writingAgent,
        IWritingAttemptRepository writingRepo,
        ISubscriptionService subscriptionService) : ControllerBase
    {
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        // POST /api/writing/evaluate
        [HttpPost("evaluate")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(WritingCorrectionResponse), 200)]
        [ProducesResponseType(402)]
        public async Task<IActionResult> Evaluate(
            [FromForm] Guid lessonId,
            [FromForm] Guid lessonPageId,
            [FromForm] Guid studentId,
            [FromForm] string childName,
            IFormFile image)
        {
            if (image is null || image.Length == 0)
                return BadRequest(new { error = "يرجى رفع صورة الكتابة." });

            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { error = "يرجى رفع صورة بصيغة PNG أو JPG." });

            if (image.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "حجم الصورة كبير جداً (الحد الأقصى 10 ميغابايت)." });

            // ── Subscription check (before Gemini OCR + correction calls) ─────────
            // studentId and lessonPageId are guaranteed non-null by the form binding above.
            var access = await subscriptionService.CheckAccessAsync(
                studentId, SubscriptionFeature.WritingEvaluation, lessonPageId);

            if (!access.IsAllowed)
                return StatusCode(402, new
                {
                    message         = access.Reason ?? "لقد وصلت إلى الحد الأقصى من محاولات الكتابة المجانية لهذا المحتوى.",
                    feature         = "WritingEvaluation",
                    requiresUpgrade = true,
                });
            // ─────────────────────────────────────────────────────────────────────

            var result = await writingAgent.EvaluateAsync(lessonPageId, lessonId, studentId, childName, image);
            return Ok(result);
        }

        // POST /api/writing/canvas
        [HttpPost("canvas")]
        [ProducesResponseType(typeof(WritingCorrectionResponse), 200)]
        public async Task<IActionResult> EvaluateCanvas([FromBody] CanvasEvaluationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageBase64))
                return BadRequest(new { error = "الرجاء توفير صورة الكتابة (base64)." });

            if (string.IsNullOrWhiteSpace(request.ExpectedText))
                return BadRequest(new { error = "الرجاء توفير الجملة المطلوبة." });

            var result = await writingAgent.EvaluateDirectAsync(request.ImageBase64, request.ExpectedText);
            return Ok(result);
        }

        // GET /api/writing/history/{studentId}
        [HttpGet("history/{studentId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(List<WritingAttemptHistoryDto>), 200)]
        public async Task<IActionResult> GetHistory(Guid studentId, [FromQuery] int take = 30)
        {
            var attempts = await writingRepo.GetByStudentIdAsync(studentId, Math.Min(take, 100));
            var result = attempts.Select(a => new WritingAttemptHistoryDto(
                a.Id,
                a.LessonPageId,
                a.ExpectedSentence,
                a.ExtractedText,
                a.SimilarityScore,
                a.IsAccepted,
                a.AttemptNumber,
                a.DisplayMessage,
                ParseMistakes(a.MistakesJson),
                ParseStringList(a.TipsJson),
                a.UploadedImagePath,
                a.AttemptedAt
            )).ToList();
            return Ok(result);
        }

        // GET /api/writing/mistakes/{studentId}
        [HttpGet("mistakes/{studentId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetMistakeSummary(Guid studentId)
        {
            var attempts = await writingRepo.GetByStudentIdAsync(studentId, 200);
            var allMistakes = attempts
                .SelectMany(a => ParseMistakes(a.MistakesJson))
                .GroupBy(m => m.Type)
                .Select(g => new { Type = g.Key, Count = g.Count(), Examples = g.Take(3).ToList() })
                .OrderByDescending(x => x.Count)
                .ToList();
            return Ok(new { StudentId = studentId, TotalAttempts = attempts.Count, MistakeBreakdown = allMistakes });
        }

        private static List<WritingMistakeDto> ParseMistakes(string json)
        {
            try { return JsonSerializer.Deserialize<List<WritingMistakeDto>>(json, _json) ?? []; }
            catch { return []; }
        }

        private static List<string> ParseStringList(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json, _json) ?? []; }
            catch { return []; }
        }
    }

    public record CanvasEvaluationRequest(string ImageBase64, string ExpectedText);
}
