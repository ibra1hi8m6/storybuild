using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SystemAdmin")]
public class AdminController(
    IPdfImportService pdfImportService,
    IUploadedStoryService uploadedStoryService,
    ILessonRepository lessonRepository,
    AppDbContext db,
    IAuthService authService) : ControllerBase
{
    // ── Import PDF book ────────────────────────────────────────────────────────
    [HttpPost("import-book")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportBookResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportBook(
        [FromForm] int level,
        [FromForm] string letter,
        [FromForm] string letterName,
        [FromForm] string? title,
        IFormFile pdfFile,
        CancellationToken ct)
    {
        if (level is < 1 or > 4)
            return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 4." });
        if (string.IsNullOrWhiteSpace(letter))
            return BadRequest(new { error = "يرجى إدخال الحرف." });
        if (string.IsNullOrWhiteSpace(letterName))
            return BadRequest(new { error = "يرجى إدخال اسم الحرف." });
        if (pdfFile is null || pdfFile.Length == 0)
            return BadRequest(new { error = "يرجى رفع ملف PDF." });

        try
        {
            var lesson = await pdfImportService.ImportBookAsync(
                level, letter.Trim(), letterName.Trim(),
                title?.Trim() ?? string.Empty, pdfFile, ct);
            return Ok(new ImportBookResponse(
                lesson.Id, lesson.Title, lesson.Level,
                lesson.Letter, lesson.LetterName, lesson.Pages.Count));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Get all books (admin, paginated) ─────────────────────────────────────
    [HttpGet("books")]
    [ProducesResponseType(typeof(AdminBooksPageDto), 200)]
    public async Task<IActionResult> GetAllBooks(
        [FromQuery] int? level,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 9)
    {
        var all = await lessonRepository.GetAllAsync(level, publishedOnly: false);
        var total = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(LessonMapper.ToSummary)
            .ToList();

        return Ok(new AdminBooksPageDto(
            items, total, page, pageSize,
            (int)Math.Ceiling((double)total / pageSize)));
    }

    // ── Delete book ────────────────────────────────────────────────────────────
    [HttpDelete("books/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        var deleted = await lessonRepository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound(new { error = "الكتاب غير موجود." });
    }

    // ── Update page sentence ────────────────────────────────────────────────────
    [HttpPatch("books/{bookId:guid}/pages/{pageId:guid}/sentence")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePageSentence(
        Guid bookId, Guid pageId,
        [FromBody] UpdatePageSentenceRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Sentence))
            return BadRequest(new { error = "الجملة لا يمكن أن تكون فارغة." });

        var updated = await lessonRepository.UpdatePageSentenceAsync(pageId, req.Sentence.Trim());
        return updated ? Ok(new { message = "تم تحديث الجملة." }) : NotFound(new { error = "الصفحة غير موجودة." });
    }

    // ── Create manual book ─────────────────────────────────────────────────────
    [HttpPost("books/manual")]
    [ProducesResponseType(typeof(ImportBookResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateManualBook([FromBody] CreateManualBookRequest req)
    {
        if (req.Level is < 1 or > 4)
            return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 4." });
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "يرجى إدخال عنوان الكتاب." });
        if (req.Pages is null || req.Pages.Count == 0)
            return BadRequest(new { error = "يرجى إضافة صفحة واحدة على الأقل." });
        if (req.Pages.Count > 3)
            return BadRequest(new { error = "الحد الأقصى 3 صفحات لكل كتاب." });

        var lessonId = Guid.NewGuid();
        var lesson = new Lesson
        {
            Id         = lessonId,
            Level      = req.Level,
            Letter     = req.Letter.Trim(),
            LetterName = req.LetterName.Trim(),
            Title      = req.Title.Trim()
        };

        for (var i = 0; i < req.Pages.Count; i++)
        {
            var pageNumber = i + 1;
            lesson.Pages.Add(new LessonPage
            {
                LessonId    = lessonId,
                PageNumber  = pageNumber,
                Sentence    = req.Pages[i].Sentence.Trim(),
                ImagePath   = string.Empty,
                IsCoverPage = pageNumber == 1,
                IsUnlocked  = pageNumber <= 2
            });
        }

        var saved = await lessonRepository.CreateManualAsync(lesson);
        return Ok(new ImportBookResponse(
            saved.Id, saved.Title, saved.Level,
            saved.Letter, saved.LetterName, saved.Pages.Count));
    }

    // ── Get book detail (admin) ────────────────────────────────────────────────
    [HttpGet("books/{id:guid}")]
    [ProducesResponseType(typeof(LessonDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBookDetail(Guid id)
    {
        var lesson = await lessonRepository.GetByIdAsync(id);
        if (lesson is null)
            return NotFound(new { error = "الكتاب غير موجود." });
        return Ok(LessonMapper.ToDetail(lesson));
    }

    // ── Publish / Unpublish lesson ─────────────────────────────────────────────
    [HttpPost("books/{id:guid}/publish")]
    public async Task<IActionResult> PublishLesson(Guid id)
    {
        var lesson = await db.Lessons.FindAsync(id);
        if (lesson is null) return NotFound(new { error = "الدرس غير موجود." });
        lesson.IsPublished = true;
        lesson.Status = ContentStatus.Published;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم نشر الدرس.", isPublished = true });
    }

    [HttpPost("books/{id:guid}/unpublish")]
    public async Task<IActionResult> UnpublishLesson(Guid id)
    {
        var lesson = await db.Lessons.FindAsync(id);
        if (lesson is null) return NotFound(new { error = "الدرس غير موجود." });
        lesson.IsPublished = false;
        lesson.Status = ContentStatus.Draft;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم إلغاء نشر الدرس.", isPublished = false });
    }

    // ── Publish / Unpublish story ──────────────────────────────────────────────
    [HttpPost("stories/{id:guid}/publish")]
    public async Task<IActionResult> PublishStory(Guid id)
    {
        var story = await db.Stories.FindAsync(id);
        if (story is null) return NotFound(new { error = "القصة غير موجودة." });
        story.IsPublished = true;
        story.Status = ContentStatus.Published;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم نشر القصة.", isPublished = true });
    }

    [HttpPost("stories/{id:guid}/unpublish")]
    public async Task<IActionResult> UnpublishStory(Guid id)
    {
        var story = await db.Stories.FindAsync(id);
        if (story is null) return NotFound(new { error = "القصة غير موجودة." });
        story.IsPublished = false;
        story.Status = ContentStatus.Draft;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم إلغاء نشر القصة.", isPublished = false });
    }

    // ── List all stories (admin) ────────────────────────────────────────────────
    [HttpGet("stories")]
    public async Task<IActionResult> GetAllStories()
    {
        var stories = await db.Stories
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                id          = s.Id,
                title       = s.Title,
                childName   = s.ChildName,
                isPublished = s.IsPublished,
                status      = s.Status.ToString(),
                source      = s.Source.ToString(),
                isApproved  = s.IsApproved,
                createdAt   = s.CreatedAt
            })
            .ToListAsync();
        return Ok(stories);
    }

    // ── AI Settings ────────────────────────────────────────────────────────────
    [HttpGet("ai-settings")]
    [ProducesResponseType(200)]
    public IActionResult GetAiSettings()
    {
        return Ok(new
        {
            model        = "gpt-4o",
            temperature  = 0.7,
            ragEnabled   = true,
            systemPrompt = "أنت مساعد تعليمي متخصص في تعليم اللغة العربية للأطفال.",
            topK         = 5
        });
    }

    [HttpPut("ai-settings")]
    [ProducesResponseType(200)]
    public IActionResult SaveAiSettings([FromBody] JsonElement settings)
    {
        return Ok(settings);
    }

    // ── Subscriptions stats ────────────────────────────────────────────────────
    [HttpGet("subscriptions/stats")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetSubscriptionStats()
    {
        var totalUsers = await db.Users.CountAsync();

        return Ok(new
        {
            totalRevenue       = 0m,
            freeCount          = totalUsers,
            proCount           = 0,
            teamCount          = 0,
            recentSubscribers  = Array.Empty<object>(),
            plans = new[]
            {
                new { id = "free",  name = "مجاني",   price = 0m,  color = "#10B981", subscribers = totalUsers },
                new { id = "pro",   name = "احترافي", price = 49m, color = "#F4788A", subscribers = 0 },
                new { id = "team",  name = "مدرسي",   price = 149m,color = "#A78BFA", subscribers = 0 }
            }
        });
    }

    // ── Users management ───────────────────────────────────────────────────────
    [HttpGet("users")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                id        = u.Id,
                name      = u.Name,
                email     = u.Email,
                role      = u.Role.ToString().ToLower(),
                createdAt = u.CreatedAt,
                isBlocked = u.IsBlocked,
                isActive  = u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users/{id:guid}/block")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> BlockUser(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(new { error = "المستخدم غير موجود." });
        user.IsBlocked = true;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حظر المستخدم." });
    }

    [HttpPost("users/{id:guid}/unblock")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UnblockUser(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(new { error = "المستخدم غير موجود." });
        user.IsBlocked = false;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم إلغاء حظر المستخدم." });
    }

    // ── List schools ──────────────────────────────────────────────────────────
    [HttpGet("schools")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetSchools()
    {
        var schools = await db.Users
            .Where(u => u.Role == UserRole.SchoolAdmin)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                id         = u.Id,
                schoolName = u.Name,
                adminEmail = u.Email,
                schoolManagerId = u.Id,
                createdAt  = u.CreatedAt,
                isBlocked  = u.IsBlocked
            })
            .ToListAsync();

        return Ok(schools);
    }

    // ── Create school admin account ────────────────────────────────────────────
    [HttpPost("schools")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.SchoolName))
            return BadRequest(new { error = "يرجى إدخال اسم المدرسة." });
        if (string.IsNullOrWhiteSpace(req.AdminEmail))
            return BadRequest(new { error = "يرجى إدخال البريد الإلكتروني." });
        if (string.IsNullOrWhiteSpace(req.AdminPassword) || req.AdminPassword.Length < 6)
            return BadRequest(new { error = "كلمة المرور يجب أن تكون 6 أحرف على الأقل." });

        try
        {
            var id = await authService.CreateSchoolAdminAsync(
                req.SchoolName, req.AdminEmail, req.AdminPassword);

            return Ok(new
            {
                id,
                schoolName      = req.SchoolName.Trim(),
                adminEmail      = req.AdminEmail.Trim().ToLower(),
                schoolManagerId = id,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Uploaded PDF Stories ───────────────────────────────────────────────────

    [HttpPost("uploaded-stories")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadStory(
        [FromForm] string title,
        IFormFile pdfFile,
        CancellationToken ct)
    {
        if (pdfFile is null || pdfFile.Length == 0)
            return BadRequest(new { error = "يرجى رفع ملف PDF." });
        try
        {
            var dto = await uploadedStoryService.ImportAsync(title, pdfFile, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("uploaded-stories")]
    public async Task<IActionResult> GetUploadedStories()
    {
        var list = await uploadedStoryService.GetAllAsync();
        return Ok(list);
    }

    [HttpDelete("uploaded-stories/{id:guid}")]
    public async Task<IActionResult> DeleteUploadedStory(Guid id)
    {
        var deleted = await uploadedStoryService.DeleteAsync(id);
        return deleted ? Ok(new { message = "تم الحذف." }) : NotFound(new { error = "القصة غير موجودة." });
    }
}

public record CreateSchoolRequest(string SchoolName, string AdminEmail, string AdminPassword);
