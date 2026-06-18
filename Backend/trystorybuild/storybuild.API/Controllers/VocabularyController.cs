using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/vocabulary")]
    [Authorize]
    public class VocabularyController(IVocabularyRepository repo) : ControllerBase
    {
        // GET /api/vocabulary/lesson/{lessonId}
        [HttpGet("lesson/{lessonId:guid}")]
        public async Task<ActionResult<List<LessonVocabularyDto>>> GetByLesson(Guid lessonId)
        {
            var list = await repo.GetByLessonAsync(lessonId);
            return Ok(list.Select(MapVocab).ToList());
        }

        // POST /api/vocabulary/lesson
        [HttpPost("lesson")]
        [Authorize(Roles = "teacher,schooladmin,systemadmin")]
        public async Task<ActionResult<LessonVocabularyDto>> AddToLesson([FromBody] AddLessonVocabularyRequest req)
        {
            var entity = new LessonVocabulary
            {
                LessonId   = req.LessonId,
                Word       = req.Word,
                Definition = req.Definition,
                ImageUrl   = req.ImageUrl,
                Order      = req.Order
            };
            var saved = await repo.SaveAsync(entity);
            return Ok(MapVocab(saved));
        }

        // DELETE /api/vocabulary/lesson/{id}
        [HttpDelete("lesson/{id:guid}")]
        [Authorize(Roles = "teacher,schooladmin,systemadmin")]
        public async Task<IActionResult> DeleteFromLesson(Guid id)
        {
            var ok = await repo.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // POST /api/vocabulary/journal
        [HttpPost("journal")]
        public async Task<ActionResult<WordJournalDto>> AddToJournal([FromBody] AddToJournalRequest req)
        {
            var entry = new WordJournalEntry
            {
                StudentId  = req.StudentId,
                Word       = req.Word,
                Definition = req.Definition,
                ImageUrl   = req.ImageUrl
            };
            var saved = await repo.AddToJournalAsync(entry);
            return Ok(MapJournal(saved));
        }

        // GET /api/vocabulary/journal/{studentId}
        [HttpGet("journal/{studentId:guid}")]
        public async Task<ActionResult<List<WordJournalDto>>> GetJournal(Guid studentId)
        {
            var list = await repo.GetJournalAsync(studentId);
            return Ok(list.Select(MapJournal).ToList());
        }

        // DELETE /api/vocabulary/journal/{id}/{studentId}
        [HttpDelete("journal/{id:guid}/{studentId:guid}")]
        public async Task<IActionResult> RemoveFromJournal(Guid id, Guid studentId)
        {
            var ok = await repo.RemoveFromJournalAsync(id, studentId);
            return ok ? NoContent() : NotFound();
        }

        private static LessonVocabularyDto MapVocab(LessonVocabulary v) =>
            new(v.Id, v.LessonId, v.Word, v.Definition, v.ImageUrl, v.Order);

        private static WordJournalDto MapJournal(WordJournalEntry e) =>
            new(e.Id, e.Word, e.Definition, e.ImageUrl, e.CreatedAt);
    }
}
