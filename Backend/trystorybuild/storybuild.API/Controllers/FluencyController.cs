using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/fluency")]
    [Authorize]
    public class FluencyController(
        IFluencyAssessorAgent fluencyAgent,
        IAudioRecordingRepository recordingRepository) : ControllerBase
    {
        // POST /api/fluency/evaluate
        [HttpPost("evaluate")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<FluencyReportDto>> Evaluate(
            [FromForm] string? pageId,
            [FromForm] string? pageType,
            [FromForm] string? expectedText,
            IFormFile? audio)
        {
            if (audio is null || audio.Length == 0)
                return BadRequest(new { error = "Audio file is required." });
            if (string.IsNullOrWhiteSpace(expectedText))
                return BadRequest(new { error = "Expected text is required." });
            if (!Guid.TryParse(pageId, out var pageGuid))
                return BadRequest(new { error = "Invalid pageId." });

            var studentId = CurrentUserId();
            var report = await fluencyAgent.EvaluateReadingAsync(
                studentId, pageGuid, pageType ?? "Story", audio, expectedText);
            return Ok(report);
        }

        // GET /api/fluency/student/{studentId}
        [HttpGet("student/{studentId:guid}")]
        public async Task<ActionResult<List<FluencyHistoryDto>>> GetStudentHistory(Guid studentId)
        {
            var recordings = await recordingRepository.GetByStudentIdAsync(studentId);
            var result = recordings.Select(r => new FluencyHistoryDto(
                r.Id,
                r.PageId,
                r.PageType,
                r.AudioFileUrl,
                r.DurationSeconds,
                r.Report?.WCPM ?? 0,
                r.Report?.AccuracyScore ?? 0,
                (r.Report?.AccuracyScore ?? 0) >= 70,
                r.CreatedAt
            )).ToList();
            return Ok(result);
        }

        // GET /api/fluency/page/{pageId}/student/{studentId}
        [HttpGet("page/{pageId:guid}/student/{studentId:guid}")]
        public async Task<ActionResult<List<FluencyHistoryDto>>> GetPageHistory(
            Guid pageId, Guid studentId)
        {
            var recordings = await recordingRepository.GetByPageIdAsync(pageId, studentId);
            var result = recordings.Select(r => new FluencyHistoryDto(
                r.Id, r.PageId, r.PageType, r.AudioFileUrl, r.DurationSeconds,
                r.Report?.WCPM ?? 0, r.Report?.AccuracyScore ?? 0,
                (r.Report?.AccuracyScore ?? 0) >= 70, r.CreatedAt
            )).ToList();
            return Ok(result);
        }

        private Guid CurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
