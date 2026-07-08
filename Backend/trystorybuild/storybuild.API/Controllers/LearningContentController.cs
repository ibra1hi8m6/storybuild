using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.AI;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/learning")]
public class LearningContentController(AppDbContext db, CloudinaryService cloudinary, ISubscriptionService subscriptionService) : ControllerBase
{
    // ════════════════════════════════════════════════════════════
    //  LETTERS
    // ════════════════════════════════════════════════════════════

    [HttpGet("letters")]
    [ProducesResponseType(typeof(List<LetterContentDto>), 200)]
    public async Task<IActionResult> GetLetters([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        List<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Letters);
            if (access.IsFree)
                freeIds = await db.LetterContents
                    .Where(l => l.IsPublished)
                    .OrderBy(l => l.SortOrder)
                    .Take(3)
                    .Select(l => l.Id)
                    .ToListAsync();
        }

        var query = db.LetterContents.Where(l => l.IsPublished);
        if (freeIds is not null)
            query = query.Where(l => freeIds.Contains(l.Id));

        var items = await query
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.CreatedAt)
            .Select(l => Map(l))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("letters/all")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(List<LetterContentDto>), 200)]
    public async Task<IActionResult> GetAllLetters()
    {
        var items = await db.LetterContents
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.CreatedAt)
            .Select(l => Map(l))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("letters/{id:guid}")]
    [ProducesResponseType(typeof(LetterContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetLetter(Guid id, [FromQuery] Guid? studentId = null)
    {
        var item = await db.LetterContents.FindAsync(id);
        if (item is null) return NotFound();

        var sid = ResolveStudentId(studentId);
        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Letters, id);
            if (!access.IsAllowed)
                return StatusCode(402, new { message = access.Reason ?? "هذا الحرف خارج نطاق الخطة المجانية.", feature = "Letters", requiresUpgrade = true });
        }

        return Ok(Map(item));
    }

    [HttpPost("letters")]
    [Authorize(Roles = "SystemAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(LetterContentDto), 201)]
    public async Task<IActionResult> CreateLetter([FromForm] LetterFormRequest req)
    {
        var entity = new LetterContent
        {
            Letter          = req.Letter,
            LetterName      = req.LetterName,
            ExampleWord     = req.ExampleWord,
            DisplaySentence = req.DisplaySentence,
            AudioText       = req.AudioText,
            WritingTarget   = req.WritingTarget,
            IsPublished     = req.IsPublished,
            SortOrder       = req.SortOrder
        };

        if (req.Image is not null)
            entity.ImagePath = await UploadImage(req.Image, "lughati/letters");

        db.LetterContents.Add(entity);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLetter), new { id = entity.Id }, Map(entity));
    }

    [HttpPut("letters/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(LetterContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateLetter(Guid id, [FromForm] LetterFormRequest req)
    {
        var entity = await db.LetterContents.FindAsync(id);
        if (entity is null) return NotFound();

        entity.Letter          = req.Letter;
        entity.LetterName      = req.LetterName;
        entity.ExampleWord     = req.ExampleWord;
        entity.DisplaySentence = req.DisplaySentence;
        entity.AudioText       = req.AudioText;
        entity.WritingTarget   = req.WritingTarget;
        entity.IsPublished     = req.IsPublished;
        entity.SortOrder       = req.SortOrder;

        if (req.Image is not null)
            entity.ImagePath = await UploadImage(req.Image, "lughati/letters");

        await db.SaveChangesAsync();
        return Ok(Map(entity));
    }

    [HttpPatch("letters/{id:guid}/publish")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleLetterPublish(Guid id, [FromQuery] bool published)
    {
        var entity = await db.LetterContents.FindAsync(id);
        if (entity is null) return NotFound();
        entity.IsPublished = published;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("letters/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteLetter(Guid id)
    {
        var entity = await db.LetterContents.FindAsync(id);
        if (entity is null) return NotFound();
        db.LetterContents.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ════════════════════════════════════════════════════════════
    //  WORDS
    // ════════════════════════════════════════════════════════════

    [HttpGet("words")]
    [ProducesResponseType(typeof(List<WordContentDto>), 200)]
    public async Task<IActionResult> GetWords([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        List<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Words);
            if (access.IsFree)
                freeIds = await db.WordContents
                    .Where(w => w.IsPublished)
                    .OrderBy(w => w.SortOrder)
                    .Take(3)
                    .Select(w => w.Id)
                    .ToListAsync();
        }

        var query = db.WordContents.Where(w => w.IsPublished);
        if (freeIds is not null)
            query = query.Where(w => freeIds.Contains(w.Id));

        var items = await query
            .OrderBy(w => w.RelatedLetter)
            .ThenBy(w => w.SortOrder)
            .Select(w => MapWord(w))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("words/all")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(List<WordContentDto>), 200)]
    public async Task<IActionResult> GetAllWords()
    {
        var items = await db.WordContents
            .OrderBy(w => w.RelatedLetter)
            .ThenBy(w => w.SortOrder)
            .Select(w => MapWord(w))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("words/by-letter/{letter}")]
    [ProducesResponseType(typeof(List<WordContentDto>), 200)]
    public async Task<IActionResult> GetWordsByLetter(string letter, [FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        List<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Words);
            if (access.IsFree)
                freeIds = await db.WordContents
                    .Where(w => w.IsPublished)
                    .OrderBy(w => w.SortOrder)
                    .Take(3)
                    .Select(w => w.Id)
                    .ToListAsync();
        }

        var query = db.WordContents.Where(w => w.IsPublished && w.RelatedLetter == letter);
        if (freeIds is not null)
            query = query.Where(w => freeIds.Contains(w.Id));

        var items = await query
            .OrderBy(w => w.SortOrder)
            .Select(w => MapWord(w))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("words/letters")]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetWordLetters([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        List<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Words);
            if (access.IsFree)
                freeIds = await db.WordContents
                    .Where(w => w.IsPublished)
                    .OrderBy(w => w.SortOrder)
                    .Take(3)
                    .Select(w => w.Id)
                    .ToListAsync();
        }

        var query = db.WordContents.Where(w => w.IsPublished);
        if (freeIds is not null)
            query = query.Where(w => freeIds.Contains(w.Id));

        var letters = await query
            .Select(w => w.RelatedLetter)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
        return Ok(letters);
    }

    [HttpGet("words/{id:guid}")]
    [ProducesResponseType(typeof(WordContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetWord(Guid id, [FromQuery] Guid? studentId = null)
    {
        var item = await db.WordContents.FindAsync(id);
        if (item is null) return NotFound();

        var sid = ResolveStudentId(studentId);
        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Words, id);
            if (!access.IsAllowed)
                return StatusCode(402, new { message = access.Reason ?? "هذه الكلمة خارج نطاق الخطة المجانية.", feature = "Words", requiresUpgrade = true });
        }

        var next = await db.WordContents
            .Where(w => w.IsPublished && (
                string.Compare(w.RelatedLetter, item.RelatedLetter) > 0 ||
                (w.RelatedLetter == item.RelatedLetter && w.SortOrder > item.SortOrder) ||
                (w.RelatedLetter == item.RelatedLetter && w.SortOrder == item.SortOrder && w.CreatedAt > item.CreatedAt)
            ))
            .OrderBy(w => w.RelatedLetter)
            .ThenBy(w => w.SortOrder)
            .ThenBy(w => w.CreatedAt)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync();

        var dto = MapWord(item);
        dto.NextId = next;
        return Ok(dto);
    }

    [HttpPost("words")]
    [Authorize(Roles = "SystemAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(WordContentDto), 201)]
    public async Task<IActionResult> CreateWord([FromForm] WordFormRequest req)
    {
        var entity = new WordContent
        {
            DisplayWord   = req.DisplayWord,
            AudioText     = req.AudioText,
            RelatedLetter = req.RelatedLetter,
            IsPublished   = req.IsPublished,
            SortOrder     = req.SortOrder
        };

        if (req.Image is not null)
            entity.ImagePath = await UploadImage(req.Image, "lughati/words");

        db.WordContents.Add(entity);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetWord), new { id = entity.Id }, MapWord(entity));
    }

    [HttpPut("words/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(WordContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateWord(Guid id, [FromForm] WordFormRequest req)
    {
        var entity = await db.WordContents.FindAsync(id);
        if (entity is null) return NotFound();

        entity.DisplayWord   = req.DisplayWord;
        entity.AudioText     = req.AudioText;
        entity.RelatedLetter = req.RelatedLetter;
        entity.IsPublished   = req.IsPublished;
        entity.SortOrder     = req.SortOrder;

        if (req.Image is not null)
            entity.ImagePath = await UploadImage(req.Image, "lughati/words");

        await db.SaveChangesAsync();
        return Ok(MapWord(entity));
    }

    [HttpDelete("words/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteWord(Guid id)
    {
        var entity = await db.WordContents.FindAsync(id);
        if (entity is null) return NotFound();
        db.WordContents.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ════════════════════════════════════════════════════════════
    //  SENTENCES
    // ════════════════════════════════════════════════════════════

    [HttpGet("sentences")]
    [ProducesResponseType(typeof(List<SentenceContentDto>), 200)]
    public async Task<IActionResult> GetSentences([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        List<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Sentences);
            if (access.IsFree)
                freeIds = await db.SentenceContents
                    .Where(s => s.IsPublished)
                    .OrderBy(s => s.SortOrder)
                    .Take(3)
                    .Select(s => s.Id)
                    .ToListAsync();
        }

        var query = db.SentenceContents.Where(s => s.IsPublished);
        if (freeIds is not null)
            query = query.Where(s => freeIds.Contains(s.Id));

        var items = await query
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.CreatedAt)
            .Select(s => MapSentence(s))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("sentences/all")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(List<SentenceContentDto>), 200)]
    public async Task<IActionResult> GetAllSentences()
    {
        var items = await db.SentenceContents
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.CreatedAt)
            .Select(s => MapSentence(s))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("sentences/{id:guid}")]
    [ProducesResponseType(typeof(SentenceContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSentence(Guid id, [FromQuery] Guid? studentId = null)
    {
        var item = await db.SentenceContents.FindAsync(id);
        if (item is null) return NotFound();

        var sid = ResolveStudentId(studentId);
        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Sentences, id);
            if (!access.IsAllowed)
                return StatusCode(402, new { message = access.Reason ?? "هذه الجملة خارج نطاق الخطة المجانية.", feature = "Sentences", requiresUpgrade = true });
        }

        var next = await db.SentenceContents
            .Where(s => s.IsPublished &&
                        (s.SortOrder > item.SortOrder ||
                        (s.SortOrder == item.SortOrder && s.CreatedAt > item.CreatedAt)))
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.CreatedAt)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        var dto = MapSentence(item);
        dto.NextId = next;
        return Ok(dto);
    }

    [HttpPost("sentences")]
    [Authorize(Roles = "SystemAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SentenceContentDto), 201)]
    public async Task<IActionResult> CreateSentence([FromForm] SentenceFormRequest req)
    {
        var entity = new SentenceContent
        {
            Option1            = req.Option1,
            Option1Audio       = req.Option1Audio,
            Option2            = req.Option2,
            Option2Audio       = req.Option2Audio,
            Option3            = req.Option3,
            Option3Audio       = req.Option3Audio,
            CorrectOptionIndex = req.CorrectOptionIndex,
            IsPublished        = req.IsPublished,
            SortOrder          = req.SortOrder
        };

        if (req.Image is not null)
            entity.ImagePath = await UploadImage(req.Image, "lughati/sentences");

        db.SentenceContents.Add(entity);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSentence), new { id = entity.Id }, MapSentence(entity));
    }

    [HttpPut("sentences/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SentenceContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSentence(Guid id, [FromForm] SentenceFormRequest req)
    {
        var entity = await db.SentenceContents.FindAsync(id);
        if (entity is null) return NotFound();

        entity.Option1            = req.Option1;
        entity.Option1Audio       = req.Option1Audio;
        entity.Option2            = req.Option2;
        entity.Option2Audio       = req.Option2Audio;
        entity.Option3            = req.Option3;
        entity.Option3Audio       = req.Option3Audio;
        entity.CorrectOptionIndex = req.CorrectOptionIndex;
        entity.IsPublished        = req.IsPublished;
        entity.SortOrder          = req.SortOrder;

        if (req.Image is not null)
            entity.ImagePath = await UploadImage(req.Image, "lughati/sentences");

        await db.SaveChangesAsync();
        return Ok(MapSentence(entity));
    }

    [HttpDelete("sentences/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteSentence(Guid id)
    {
        var entity = await db.SentenceContents.FindAsync(id);
        if (entity is null) return NotFound();
        db.SentenceContents.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ════════════════════════════════════════════════════════════
    //  CATALOG ENDPOINTS (full list + isLocked for frontend UX)
    // ════════════════════════════════════════════════════════════

    [HttpGet("letters/catalog")]
    public async Task<IActionResult> GetLettersCatalog([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        HashSet<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Letters);
            if (access.IsFree)
            {
                var ids = await db.LetterContents
                    .Where(l => l.IsPublished)
                    .OrderBy(l => l.SortOrder).ThenBy(l => l.CreatedAt)
                    .Take(SubscriptionConstants.FreeLettersLimit)
                    .Select(l => l.Id)
                    .ToListAsync();
                freeIds = ids.ToHashSet();
            }
        }

        var items = await db.LetterContents
            .Where(l => l.IsPublished)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(l => new
        {
            id          = l.Id,
            letter      = l.Letter,
            letterName  = l.LetterName,
            exampleWord = l.ExampleWord,
            imagePath   = l.ImagePath,
            sortOrder   = l.SortOrder,
            isLocked    = freeIds is not null && !freeIds.Contains(l.Id),
        }));
    }

    [HttpGet("words/catalog")]
    public async Task<IActionResult> GetWordsCatalog([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        HashSet<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Words);
            if (access.IsFree)
            {
                var ids = await db.WordContents
                    .Where(w => w.IsPublished)
                    .OrderBy(w => w.SortOrder).ThenBy(w => w.CreatedAt)
                    .Take(SubscriptionConstants.FreeWordsLimit)
                    .Select(w => w.Id)
                    .ToListAsync();
                freeIds = ids.ToHashSet();
            }
        }

        var items = await db.WordContents
            .Where(w => w.IsPublished)
            .OrderBy(w => w.RelatedLetter).ThenBy(w => w.SortOrder)
            .ToListAsync();

        return Ok(items.Select(w => new
        {
            id            = w.Id,
            displayWord   = w.DisplayWord,
            relatedLetter = w.RelatedLetter,
            imagePath     = w.ImagePath,
            sortOrder     = w.SortOrder,
            isLocked      = freeIds is not null && !freeIds.Contains(w.Id),
        }));
    }

    [HttpGet("sentences/catalog")]
    public async Task<IActionResult> GetSentencesCatalog([FromQuery] Guid? studentId = null)
    {
        var sid = ResolveStudentId(studentId);
        HashSet<Guid>? freeIds = null;

        if (sid.HasValue)
        {
            var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Sentences);
            if (access.IsFree)
            {
                var ids = await db.SentenceContents
                    .Where(s => s.IsPublished)
                    .OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt)
                    .Take(SubscriptionConstants.FreeSentencesLimit)
                    .Select(s => s.Id)
                    .ToListAsync();
                freeIds = ids.ToHashSet();
            }
        }

        var items = await db.SentenceContents
            .Where(s => s.IsPublished)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(s => new
        {
            id        = s.Id,
            imagePath = s.ImagePath,
            option1   = s.Option1,
            option2   = s.Option2,
            option3   = s.Option3,
            sortOrder = s.SortOrder,
            isLocked  = freeIds is not null && !freeIds.Contains(s.Id),
        }));
    }

    // ════════════════════════════════════════════════════════════
    //  LEARNING ATTEMPTS
    // ════════════════════════════════════════════════════════════

    [HttpPost("attempts")]
    [ProducesResponseType(typeof(LearningAttemptDto), 201)]
    public async Task<IActionResult> SaveAttempt([FromBody] SaveLearningAttemptRequest req)
    {
        var entity = new LearningAttempt
        {
            ChildName    = req.ChildName,
            StudentId    = req.StudentId,
            ContentType  = req.ContentType,
            ContentId    = req.ContentId,
            AttemptType  = req.AttemptType,
            ExpectedText = req.ExpectedText,
            DetectedText = req.DetectedText,
            Score        = req.Score,
            IsCorrect    = req.IsCorrect,
            FeedbackText = req.FeedbackText,
            FeedbackAudio = req.FeedbackAudio
        };

        db.LearningAttempts.Add(entity);

        // Record first-success completion (idempotent — unique index prevents duplicates)
        if (req.IsCorrect && req.StudentId.HasValue)
        {
            var completionType = req.ContentType switch
            {
                LearningContentType.LetterSound       => ContentCompletionType.Letter,
                LearningContentType.LetterRecognition => ContentCompletionType.Letter,
                LearningContentType.WordPractice      => ContentCompletionType.Word,
                LearningContentType.SentencePractice  => ContentCompletionType.Sentence,
                _ => (ContentCompletionType?)null
            };

            if (completionType.HasValue)
            {
                var alreadyDone = await db.StudentContentCompletions.AnyAsync(c =>
                    c.StudentId   == req.StudentId.Value &&
                    c.ContentType == completionType.Value &&
                    c.ContentId   == req.ContentId);

                if (!alreadyDone)
                    db.StudentContentCompletions.Add(new StudentContentCompletion
                    {
                        StudentId   = req.StudentId.Value,
                        ContentType = completionType.Value,
                        ContentId   = req.ContentId
                    });
            }
        }

        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAttempts), new { studentId = entity.StudentId }, MapAttempt(entity));
    }

    [HttpGet("attempts/{studentId:guid}")]
    [ProducesResponseType(typeof(List<LearningAttemptDto>), 200)]
    public async Task<IActionResult> GetAttempts(Guid studentId, [FromQuery] LearningContentType? contentType)
    {
        var query = db.LearningAttempts.Where(a => a.StudentId == studentId);
        if (contentType.HasValue)
            query = query.Where(a => a.ContentType == contentType.Value);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => MapAttempt(a))
            .ToListAsync();
        return Ok(items);
    }

    // ════════════════════════════════════════════════════════════
    //  STORY PAGE SELECTION (for admin PDF import)
    // ════════════════════════════════════════════════════════════

    [HttpPut("story-pages/{pageId:guid}/select")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStoryPage(Guid pageId, [FromQuery] bool isStoryPage)
    {
        var page = await db.StoryPages.FindAsync(pageId);
        if (page is null) return NotFound();
        page.IsStoryPage = isStoryPage;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("story-pages/{pageId:guid}/audio")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStoryPageAudio(Guid pageId, [FromBody] UpdatePageAudioRequest req)
    {
        var page = await db.StoryPages.FindAsync(pageId);
        if (page is null) return NotFound();
        page.AudioText = req.AudioText;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("lesson-pages/{pageId:guid}/audio")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateLessonPageAudio(Guid pageId, [FromBody] UpdatePageAudioRequest req)
    {
        var page = await db.LessonPages.FindAsync(pageId);
        if (page is null) return NotFound();
        page.AudioText = req.AudioText;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves a studentId from JWT (Student role) → query param → null.
    /// Returns null when neither source is available; callers allow access in that case.
    /// </summary>
    private Guid? ResolveStudentId(Guid? queryStudentId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Student")
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(raw, out var jwtId))
                return jwtId;
        }
        return queryStudentId;
    }

    private async Task<string> UploadImage(IFormFile file, string folder)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return await cloudinary.UploadImageBytesAsync(ms.ToArray(), Guid.NewGuid().ToString(), folder);
    }

    private static LetterContentDto Map(LetterContent l) => new()
    {
        Id              = l.Id,
        Letter          = l.Letter,
        LetterName      = l.LetterName,
        ExampleWord     = l.ExampleWord,
        DisplaySentence = l.DisplaySentence,
        AudioText       = l.AudioText,
        WritingTarget   = l.WritingTarget,
        ImagePath       = l.ImagePath,
        IsPublished     = l.IsPublished,
        SortOrder       = l.SortOrder
    };

    private static WordContentDto MapWord(WordContent w) => new()
    {
        Id            = w.Id,
        DisplayWord   = w.DisplayWord,
        AudioText     = w.AudioText,
        RelatedLetter = w.RelatedLetter,
        ImagePath     = w.ImagePath,
        IsPublished   = w.IsPublished,
        SortOrder     = w.SortOrder
    };

    private static SentenceContentDto MapSentence(SentenceContent s) => new()
    {
        Id                 = s.Id,
        ImagePath          = s.ImagePath,
        Option1            = s.Option1,
        Option1Audio       = s.Option1Audio,
        Option2            = s.Option2,
        Option2Audio       = s.Option2Audio,
        Option3            = s.Option3,
        Option3Audio       = s.Option3Audio,
        CorrectOptionIndex = s.CorrectOptionIndex,
        IsPublished        = s.IsPublished,
        SortOrder          = s.SortOrder
    };

    private static LearningAttemptDto MapAttempt(LearningAttempt a) => new()
    {
        Id           = a.Id,
        ChildName    = a.ChildName,
        ContentType  = a.ContentType,
        ContentId    = a.ContentId,
        AttemptType  = a.AttemptType,
        ExpectedText = a.ExpectedText,
        DetectedText = a.DetectedText,
        Score        = a.Score,
        IsCorrect    = a.IsCorrect,
        FeedbackText = a.FeedbackText,
        FeedbackAudio = a.FeedbackAudio,
        CreatedAt    = a.CreatedAt
    };
}

public class UpdatePageAudioRequest
{
    public string? AudioText { get; set; }
}

public class LetterFormRequest : UpsertLetterContentRequest
{
    public IFormFile? Image { get; set; }
}

public class WordFormRequest : UpsertWordContentRequest
{
    public IFormFile? Image { get; set; }
}

public class SentenceFormRequest : UpsertSentenceContentRequest
{
    public IFormFile? Image { get; set; }
}
