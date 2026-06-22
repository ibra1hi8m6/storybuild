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
        [HttpGet("{storyId:guid}/{childName}")]
        [ProducesResponseType(typeof(ProgressResponse), 200)]
        public async Task<IActionResult> Get(Guid storyId, string childName)
        {
            var progress = await progressRepository.GetAsync(storyId, childName);
            if (progress is null)
                return Ok(new ProgressResponse(storyId, childName, 1, 0, 0, 0, false));

            return Ok(new ProgressResponse(
                progress.StoryId ?? storyId,
                progress.ChildName,
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
            var progress = new StudentProgress
            {
                StoryId = request.StoryId,
                ChildName = request.ChildName,
                CurrentPage = request.CurrentPage,
                TotalQuestions = request.TotalQuestions,
                CorrectAnswers = request.CorrectAnswers,
                ScorePercentage = request.ScorePercentage,
                ExamCompleted = request.ExamCompleted
            };

            await progressRepository.SaveAsync(progress);
            return Ok(request);
        }

        /// <summary>Update lesson exam progress after student submits lesson exam.</summary>
        [HttpPut("lesson")]
        [ProducesResponseType(typeof(LessonProgressRequest), 200)]
        public async Task<IActionResult> UpdateLesson([FromBody] LessonProgressRequest request)
        {
            var progress = new StudentProgress
            {
                LessonId        = request.LessonId,
                ChildName       = request.ChildName,
                TotalQuestions  = request.TotalQuestions,
                CorrectAnswers  = request.CorrectAnswers,
                ScorePercentage = request.ScorePercentage,
                ExamCompleted   = request.ExamCompleted
            };

            await progressRepository.SaveAsync(progress);
            await UpdateWeaknessMapAsync(request.ChildName, request.LessonId, request.CorrectAnswers, request.TotalQuestions);
            return Ok(request);
        }

        // ── POST /api/progress/page ────────────────────────────────────────────
        [HttpPost("page")]
        public async Task<IActionResult> MarkPageDone([FromBody] MarkPageRequest req)
        {
            var exists = await db.LessonPageCompletions.AnyAsync(
                c => c.ChildName == req.ChildName && c.LessonPageId == req.LessonPageId);
            if (!exists)
            {
                db.LessonPageCompletions.Add(new Domain.Entities.LessonPageCompletion
                {
                    ChildName        = req.ChildName,
                    LessonId         = req.LessonId,
                    LessonPageId     = req.LessonPageId,
                    WritingSubmitted = req.WritingSubmitted
                });
                await db.SaveChangesAsync();
            }
            return Ok();
        }

        // ── GET /api/progress/lesson/{lessonId}/{childName} ────────────────────
        [HttpGet("lesson/{lessonId:guid}/{childName}")]
        public async Task<IActionResult> GetLessonPageProgress(Guid lessonId, string childName)
        {
            var completedIds = await db.LessonPageCompletions
                .Where(c => c.LessonId == lessonId && c.ChildName == childName)
                .Select(c => c.LessonPageId)
                .ToListAsync();
            var total = await db.LessonPages.CountAsync(p => p.LessonId == lessonId);
            return Ok(new LessonPageProgressResponse(completedIds, completedIds.Count, total));
        }

        // ── GET /api/progress/current/{childName} ──────────────────────────────
        [HttpGet("current/{childName}")]
        public async Task<IActionResult> GetCurrentLesson(string childName)
        {
            var inProgress = await db.StudentProgress
                .Where(p => p.ChildName == childName && p.LessonId.HasValue && !p.ExamCompleted)
                .OrderByDescending(p => p.LastUpdatedAt)
                .FirstOrDefaultAsync();

            if (inProgress?.LessonId is null)
                return Ok(new CurrentLessonResponse(null, null, 1, 0, 1));

            var lesson = await db.Lessons.Include(l => l.Pages)
                .FirstOrDefaultAsync(l => l.Id == inProgress.LessonId.Value);
            if (lesson is null)
                return Ok(new CurrentLessonResponse(null, null, 1, 0, 1));

            var completedCount = await db.LessonPageCompletions
                .CountAsync(c => c.ChildName == childName && c.LessonId == lesson.Id);

            return Ok(new CurrentLessonResponse(
                lesson.Id, lesson.Title,
                Math.Min(completedCount + 1, lesson.Pages.Count),
                lesson.Pages.Count,
                lesson.Level));
        }

        // ── GET /api/progress/weakness/{childName} ─────────────────────────────
        [HttpGet("weakness/{childName}")]
        public async Task<IActionResult> GetWeaknessMap(string childName)
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.Name == childName);
            if (student is null) return NotFound();
            var map = JsonSerializer.Deserialize<WeaknessMap>(student.WeaknessMapJson ?? "{}") ?? new WeaknessMap();
            return Ok(map);
        }

        private async Task UpdateWeaknessMapAsync(string childName, Guid? lessonId, int correct, int total)
        {
            if (lessonId is null || total == 0) return;

            var student = await db.Students.FirstOrDefaultAsync(s => s.Name == childName);
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
