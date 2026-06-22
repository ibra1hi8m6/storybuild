using Application.Agent;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/story")]
    public class StoryController(
    StoryAgent storyAgent,
    IStoryRepository storyRepository,
    IUploadedStoryService uploadedStoryService) : ControllerBase
    {
        /// <summary>Generate a new 3-page Arabic story with images.</summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateStoryResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Generate([FromBody] GenerateStoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ChildName) ||
                string.IsNullOrWhiteSpace(request.Character) ||
                string.IsNullOrWhiteSpace(request.Theme))
                return BadRequest(new { error = "يرجى إرسال اسم الطفل والشخصية والموضوع." });

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

        /// <summary>List AI stories belonging to a specific child.</summary>
        [HttpGet("mine/{childName}")]
        [ProducesResponseType(typeof(List<GenerateStoryResponse>), 200)]
        public async Task<IActionResult> GetMine(string childName)
        {
            var stories = await storyRepository.GetByChildNameAsync(childName);
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
        public async Task<IActionResult> GetUploaded()
        {
            var list = await uploadedStoryService.GetAllAsync();
            return Ok(list);
        }

        /// <summary>Get a single uploaded story by ID (for journey).</summary>
        [HttpGet("uploaded/{id:guid}")]
        public async Task<IActionResult> GetUploadedById(Guid id)
        {
            var story = await uploadedStoryService.GetByIdAsync(id);
            if (story is null) return NotFound(new { error = "القصة غير موجودة." });
            return Ok(story);
        }
    }

}
