using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/progress")]
    public class ProgressController(IStudentProgressRepository progressRepository, AppDbContext db) : ControllerBase
    {
        /// <summary>Get student progress for a specific story.</summary>
        [HttpGet("{storyId:guid}/{studentId:guid}")]
        [ProducesResponseType(typeof(ProgressResponse), 200)]
        public async Task<IActionResult> Get(Guid storyId, Guid studentId)
        {
            var progress = await progressRepository.GetByStudentAsync(storyId, studentId);
            if (progress is null)
                return Ok(new ProgressResponse(storyId, studentId, 1, 0, 0, 0, false));

            return Ok(new ProgressResponse(
                progress.StoryId ?? storyId,
                progress.StudentId ?? studentId,
                progress.CurrentPage,
                progress.TotalQuestions,
                progress.CorrectAnswers,
                progress.ScorePercentage,
                progress.ExamCompleted));
        }

        /// <summary>Update student page progress.</summary>
        [HttpPut]
        [ProducesResponseType(typeof(ProgressResponse), 200)]
        public async Task<IActionResult> Update([FromBody] ProgressResponse request)
        {
            var student = await db.Students.FindAsync(request.StudentId);
            var progress = new StudentProgress
            {
                StudentId       = request.StudentId,
                ChildName       = student?.Name ?? string.Empty,
                StoryId         = request.StoryId,
                CurrentPage     = request.CurrentPage,
                TotalQuestions  = request.TotalQuestions,
                CorrectAnswers  = request.CorrectAnswers,
                ScorePercentage = request.ScorePercentage,
                ExamCompleted   = request.ExamCompleted
            };

            await progressRepository.SaveAsync(progress);
            return Ok(request);
        }

        /// <summary>Update lesson exam progress after student submits lesson exam.</summary>
        [HttpPut("lesson")]
        [ProducesResponseType(typeof(LessonProgressRequest), 200)]
        public async Task<IActionResult> UpdateLesson([FromBody] LessonProgressRequest request)
        {
            var student = await db.Students.FindAsync(request.StudentId);
            var progress = new StudentProgress
            {
                StudentId       = request.StudentId,
                ChildName       = student?.Name ?? string.Empty,
                LessonId        = request.LessonId,
                TotalQuestions  = request.TotalQuestions,
                CorrectAnswers  = request.CorrectAnswers,
                ScorePercentage = request.ScorePercentage,
                ExamCompleted   = request.ExamCompleted
            };

            await progressRepository.SaveAsync(progress);
            await UpdateWeaknessMapAsync(request.StudentId, request.LessonId, request.CorrectAnswers, request.TotalQuestions);
            return Ok(request);
        }

        // ── POST /api/progress/page ────────────────────────────────────────────
        [HttpPost("page")]
        public async Task<IActionResult> MarkPageDone([FromBody] MarkPageRequest req)
        {
            var exists = await db.LessonPageCompletions.AnyAsync(
                c => c.StudentId == req.StudentId && c.LessonPageId == req.LessonPageId);
            if (!exists)
            {
                var student = await db.Students.FindAsync(req.StudentId);
                db.LessonPageCompletions.Add(new Domain.Entities.LessonPageCompletion
                {
                    StudentId        = req.StudentId,
                    ChildName        = student?.Name ?? string.Empty,
                    LessonId         = req.LessonId,
                    LessonPageId     = req.LessonPageId,
                    WritingSubmitted = req.WritingSubmitted
                });
                await db.SaveChangesAsync();
            }
            return Ok();
        }

        // ── GET /api/progress/lesson/{lessonId}/{studentId} ────────────────────
        [HttpGet("lesson/{lessonId:guid}/{studentId:guid}")]
        public async Task<IActionResult> GetLessonPageProgress(Guid lessonId, Guid studentId)
        {
            var completedIds = await db.LessonPageCompletions
                .Where(c => c.LessonId == lessonId && c.StudentId == studentId)
                .Select(c => c.LessonPageId)
                .ToListAsync();
            var total = await db.LessonPages.CountAsync(p => p.LessonId == lessonId);
            return Ok(new LessonPageProgressResponse(completedIds, completedIds.Count, total));
        }

        // ── GET /api/progress/current/{studentId} ──────────────────────────────
        [HttpGet("current/{studentId:guid}")]
        public async Task<IActionResult> GetCurrentLesson(Guid studentId)
        {
            var inProgress = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.LessonId.HasValue && !p.ExamCompleted)
                .OrderByDescending(p => p.LastUpdatedAt)
                .FirstOrDefaultAsync();

            if (inProgress?.LessonId is null)
                return Ok(new CurrentLessonResponse(null, null, 1, 0, 1));

            var lesson = await db.Lessons.Include(l => l.Pages)
                .FirstOrDefaultAsync(l => l.Id == inProgress.LessonId.Value);
            if (lesson is null)
                return Ok(new CurrentLessonResponse(null, null, 1, 0, 1));

            var completedCount = await db.LessonPageCompletions
                .CountAsync(c => c.StudentId == studentId && c.LessonId == lesson.Id);

            return Ok(new CurrentLessonResponse(
                lesson.Id, lesson.Title,
                Math.Min(completedCount + 1, lesson.Pages.Count),
                lesson.Pages.Count,
                lesson.Level));
        }

        // ── GET /api/progress/weakness/{studentId} ─────────────────────────────
        [HttpGet("weakness/{studentId:guid}")]
        public async Task<IActionResult> GetWeaknessMap(Guid studentId)
        {
            var student = await db.Students.FindAsync(studentId);
            if (student is null) return NotFound();
            var map = JsonSerializer.Deserialize<WeaknessMap>(student.WeaknessMapJson ?? "{}") ?? new WeaknessMap();
            return Ok(map);
        }

        private async Task UpdateWeaknessMapAsync(Guid studentId, Guid? lessonId, int correct, int total)
        {
            if (lessonId is null || total == 0) return;

            var student = await db.Students.FindAsync(studentId);
            if (student is null) return;

            var lesson = await db.Lessons.FindAsync(lessonId.Value);
            if (lesson is null) return;

            var map = JsonSerializer.Deserialize<WeaknessMap>(
                student.WeaknessMapJson ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new WeaknessMap();

            // Per-letter tracking
            var letter = lesson.Letter ?? "";
            if (!string.IsNullOrEmpty(letter))
            {
                if (!map.Letters.TryGetValue(letter, out var ls)) ls = new SkillStat();
                ls.Attempts += total;
                ls.Correct  += correct;
                map.Letters[letter] = ls;
            }

            // Per-lesson tracking
            var lessonKey = lessonId.Value.ToString();
            if (!map.Lessons.TryGetValue(lessonKey, out var ll)) ll = new LessonStat();
            ll.Title    = lesson.Title;
            ll.Letter   = letter;
            ll.Attempts += total;
            ll.Correct  += correct;
            map.Lessons[lessonKey] = ll;

            student.WeaknessMapJson = JsonSerializer.Serialize(map);
            await db.SaveChangesAsync();
        }
    }

    public class WeaknessMap
    {
        public Dictionary<string, SkillStat>  Letters { get; set; } = [];
        public Dictionary<string, LessonStat> Lessons { get; set; } = [];
    }
    public class SkillStat  { public int Attempts { get; set; } public int Correct { get; set; } }
    public class LessonStat { public string Title { get; set; } = ""; public string Letter { get; set; } = ""; public int Attempts { get; set; } public int Correct { get; set; } }
}
