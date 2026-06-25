using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/placement")]
    public class PlacementController(IPlacementRepository repository, AppDbContext db) : ControllerBase
    {
        [HttpGet("questions")]
        [ProducesResponseType(typeof(List<PlacementQuestionDto>), 200)]
        public async Task<IActionResult> GetQuestions()
        {
            await repository.SeedAsync();
            var questions = await repository.GetAllAsync();
            return Ok(questions.Select(MapToDto).ToList());
        }

        [HttpPost("submit")]
        [ProducesResponseType(typeof(PlacementResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Submit([FromBody] PlacementSubmitRequest request)
        {
            if (request.Answers == null || request.Answers.Count == 0)
                return BadRequest(new { error = "يرجى إرسال الإجابات." });

            var allQuestions = await repository.GetAllAsync();
            var answerMap    = request.Answers.ToDictionary(a => a.QuestionId, a => a.Answer);

            int p1    = ScorePart(allQuestions, answerMap, 1);
            int p2    = ScorePart(allQuestions, answerMap, 2);
            int p3    = ScorePart(allQuestions, answerMap, 3);
            int total = p1 + p2 + p3;

            // Strict rule: must score 5/5 to advance; fail S1 or S2 → Level 1; fail S3 → Level 2
            int level = p1 < 5 ? 1 : (p2 < 5 ? 1 : (p3 < 5 ? 2 : 3));

            string[] levelNames = ["", "الحروف والأصوات", "الكلمات والمفردات", "الجمل والقصص"];
            string[] messages   =
            [
                "",
                "أنت في المستوى الأول! ستتعلم الحروف العربية والأصوات.",
                "رائع! أنت في المستوى الثاني! ستتعلم الكلمات والمفردات.",
                "ممتاز! أنت في المستوى الثالث! ستقرأ الجمل والقصص."
            ];

            return Ok(new PlacementResultDto(total, p1, p2, p3, level, levelNames[level], messages[level]));
        }

        private static int ScorePart(
            List<PlacementQuestion> questions,
            Dictionary<Guid, string> answerMap,
            int part) =>
            questions
                .Where(q => q.Part == part)
                .Count(q => answerMap.TryGetValue(q.Id, out var ans) && ans == q.CorrectAnswer);

        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        private static PlacementQuestionDto MapToDto(PlacementQuestion q)
        {
            var options = JsonSerializer.Deserialize<List<OptionRaw>>(q.OptionsJson, JsonOpts) ?? [];
            return new PlacementQuestionDto(
                q.Id, q.Part, q.Order,
                q.QuestionText, q.ImageContent,
                options.Select(o => new PlacementOptionDto(o.Key, o.Emoji, o.Label)).ToList(),
                q.AudioText,
                q.CorrectAnswer);
        }

        // ── POST /api/placement/retake ─────────────────────────────────────────
        // Student can retake placement ONLY after completing all lessons in their level
        [HttpPost("retake")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RequestRetake()
        {
            var studentIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(studentIdStr, out var studentId))
                return Unauthorized();

            var student = await db.Students.FindAsync(studentId);
            if (student is null) return NotFound();

            // Count total lessons in the student's current level
            int totalLessons = await db.Lessons.CountAsync(l => l.Level == student.Level);
            if (totalLessons == 0)
                return BadRequest(new { error = "لا توجد دروس في مستواك الحالي." });

            // Count lessons the student has completed (exam passed)
            int completedLessons = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.LessonId != null && p.ExamCompleted)
                .Select(p => p.LessonId)
                .Distinct()
                .Join(db.Lessons.Where(l => l.Level == student.Level), p => p, l => l.Id, (p, l) => p)
                .CountAsync();

            if (completedLessons < totalLessons)
                return BadRequest(new
                {
                    error = $"يجب إكمال جميع دروس المستوى الحالي أولاً ({completedLessons}/{totalLessons} دروس مكتملة).",
                    completed = completedLessons,
                    total = totalLessons
                });

            // Allow re-test
            student.PlacementDone = false;
            await db.SaveChangesAsync();

            return Ok(new { message = "يمكنك الآن إعادة اختبار تحديد المستوى.", studentName = student.Name });
        }

        // ── GET /api/placement/level-completion/{studentId} ────────────────────
        [HttpGet("level-completion/{studentId:guid}")]
        public async Task<IActionResult> GetLevelCompletion(Guid studentId)
        {
            var student = await db.Students.FindAsync(studentId);
            if (student is null) return NotFound();

            int totalLessons = await db.Lessons.CountAsync(l => l.Level == student.Level);
            int completedLessons = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.LessonId != null && p.ExamCompleted)
                .Select(p => p.LessonId)
                .Distinct()
                .Join(db.Lessons.Where(l => l.Level == student.Level), p => p, l => l.Id, (p, l) => p)
                .CountAsync();

            return Ok(new
            {
                level            = student.Level,
                totalLessons,
                completedLessons,
                isLevelComplete  = totalLessons > 0 && completedLessons >= totalLessons,
                placementDone    = student.PlacementDone
            });
        }

        private sealed class OptionRaw
        {
            public string Key   { get; set; } = string.Empty;
            public string Emoji { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }
    }
}
