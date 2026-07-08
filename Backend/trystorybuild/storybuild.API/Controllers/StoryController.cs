using Application.Agents;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/story")]
    public class StoryController(
    StoryAgent storyAgent,
    IStoryRepository storyRepository,
    IUploadedStoryService uploadedStoryService,
    ISubscriptionService subscriptionService) : ControllerBase
    {
        /// <summary>Generate a new 3-page Arabic story with images.</summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateStoryResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(402)]
        public async Task<IActionResult> Generate([FromBody] GenerateStoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ChildName) ||
                string.IsNullOrWhiteSpace(request.Character) ||
                string.IsNullOrWhiteSpace(request.Theme))
                return BadRequest(new { error = "يرجى إرسال اسم الطفل والشخصية والموضوع." });

            // ── Subscription check (before any AI/image generation) ───────────────
            // Prefer explicit StudentId in body; fall back to JWT sub claim if present.
            var studentId = request.StudentId
                ?? (Guid.TryParse(
                        User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        out var jwtId) ? jwtId : (Guid?)null);

            if (studentId.HasValue)
            {
                var access = await subscriptionService.CheckAccessAsync(
                    studentId.Value, SubscriptionFeature.AiStoryGeneration);

                if (!access.IsAllowed)
                    return StatusCode(402, new
                    {
                        message          = access.Reason ?? "لقد استخدمت حصتك المجانية من القصص المولّدة بالذكاء الاصطناعي.",
                        feature          = "AiStoryGeneration",
                        requiresUpgrade  = true,
                    });
            }
            // ─────────────────────────────────────────────────────────────────────

            try
            {
                var result = await storyAgent.RunAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "تعذّر توليد القصة. حاول مرة أخرى.", detail = ex.Message });
            }
        }

        /// <summary>Load a previously generated story by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GenerateStoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var story = await storyRepository.GetByIdAsync(id);
            if (story is null) return NotFound(new { error = "القصة غير موجودة." });
            return Ok(StoryAgent.MapToResponse(story));
        }

        /// <summary>List all AI-generated stories (excludes admin-uploaded PDF stories).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<GenerateStoryResponse>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var stories = await storyRepository.GetAllAsync(publishedOnly: true);
            return Ok(stories
                .Where(s => s.Source == Domain.Entities.StorySource.AiGenerated)
                .Select(StoryAgent.MapToResponse).ToList());
        }

        /// <summary>List AI stories belonging to a specific student (by UUID).</summary>
        [HttpGet("mine/{studentId:guid}")]
        [ProducesResponseType(typeof(List<GenerateStoryResponse>), 200)]
        public async Task<IActionResult> GetMine(Guid studentId)
        {
            var stories = await storyRepository.GetByStudentIdAsync(studentId);
            return Ok(stories
                .Where(s => s.Source == Domain.Entities.StorySource.AiGenerated)
                .Select(StoryAgent.MapToResponse).ToList());
        }

        /// <summary>Delete a story by ID.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await storyRepository.DeleteAsync(id);
            if (!deleted) return NotFound(new { error = "القصة غير موجودة." });
            return NoContent();
        }

        /// <summary>List all admin-uploaded PDF stories (for student browser).</summary>
        [HttpGet("uploaded")]
        public async Task<IActionResult> GetUploaded([FromQuery] Guid? studentId = null)
        {
            var list = await uploadedStoryService.GetAllAsync();

            var sid = ResolveStudentId(studentId);
            if (sid.HasValue)
            {
                var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Stories);
                if (access.IsFree)
                {
                    var firstId = list.OrderBy(s => s.CreatedAt).Select(s => s.Id).FirstOrDefault();
                    if (firstId != Guid.Empty)
                        list = list.Where(s => s.Id == firstId).ToList();
                }
            }

            return Ok(list);
        }

        /// <summary>Catalog: all uploaded stories with isLocked for frontend UX.</summary>
        [HttpGet("uploaded/catalog")]
        public async Task<IActionResult> GetUploadedCatalog([FromQuery] Guid? studentId = null)
        {
            var list = await uploadedStoryService.GetAllAsync();
            var sid  = ResolveStudentId(studentId);
            HashSet<Guid>? freeIds = null;

            if (sid.HasValue)
            {
                var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Stories);
                if (access.IsFree)
                {
                    var firstId = list.OrderBy(s => s.CreatedAt).Select(s => s.Id).FirstOrDefault();
                    if (firstId != Guid.Empty)
                        freeIds = new HashSet<Guid> { firstId };
                }
            }

            return Ok(list.Select(s => new
            {
                id            = s.Id,
                title         = s.Title,
                coverImageUrl = s.CoverImageUrl,
                pageCount     = s.PageCount,
                createdAt     = s.CreatedAt,
                isLocked      = freeIds is not null && !freeIds.Contains(s.Id),
            }));
        }

        /// <summary>Get a single uploaded story by ID (for journey).</summary>
        [HttpGet("uploaded/{id:guid}")]
        public async Task<IActionResult> GetUploadedById(Guid id, [FromQuery] Guid? studentId = null)
        {
            var story = await uploadedStoryService.GetByIdAsync(id);
            if (story is null) return NotFound(new { error = "القصة غير موجودة." });

            var sid = ResolveStudentId(studentId);
            if (sid.HasValue)
            {
                var access = await subscriptionService.CheckAccessAsync(sid.Value, SubscriptionFeature.Stories, id);
                if (!access.IsAllowed)
                    return StatusCode(402, new { message = access.Reason ?? "هذه القصة خارج نطاق الخطة المجانية.", feature = "Stories", requiresUpgrade = true });
            }

            return Ok(story);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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
    }

}
