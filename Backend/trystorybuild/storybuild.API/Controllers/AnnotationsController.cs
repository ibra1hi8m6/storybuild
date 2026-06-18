using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/annotations")]
    [Authorize]
    public class AnnotationsController(IAnnotationRepository repo) : ControllerBase
    {
        // POST /api/annotations
        [HttpPost]
        public async Task<ActionResult<AnnotationDto>> Save([FromBody] SaveAnnotationRequest req)
        {
            var entity = new Annotation
            {
                StudentId    = req.StudentId,
                PageId       = req.PageId,
                PageType     = req.PageType,
                Type         = Enum.Parse<AnnotationType>(req.Type, ignoreCase: true),
                ColorHex     = req.ColorHex,
                SelectedText = req.SelectedText,
                StartOffset  = req.StartOffset,
                EndOffset    = req.EndOffset
            };
            var saved = await repo.SaveAsync(entity);
            return Ok(Map(saved));
        }

        // GET /api/annotations/{studentId}/{pageId}
        [HttpGet("{studentId:guid}/{pageId:guid}")]
        public async Task<ActionResult<List<AnnotationDto>>> Get(Guid studentId, Guid pageId)
        {
            var list = await repo.GetByPageAsync(pageId, studentId);
            return Ok(list.Select(Map).ToList());
        }

        // DELETE /api/annotations/{id}/{studentId}
        [HttpDelete("{id:guid}/{studentId:guid}")]
        public async Task<IActionResult> Delete(Guid id, Guid studentId)
        {
            var ok = await repo.DeleteAsync(id, studentId);
            return ok ? NoContent() : NotFound();
        }

        private static AnnotationDto Map(Annotation a) => new(
            a.Id, a.StudentId, a.PageId, a.PageType,
            a.Type.ToString(), a.ColorHex, a.SelectedText,
            a.StartOffset, a.EndOffset, a.CreatedAt);
    }
}
