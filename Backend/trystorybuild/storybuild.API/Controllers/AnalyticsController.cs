using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    // ── Per-student weak letters ───────────────────────────────────────────────
    [HttpGet("student/{studentId:guid}/weak-letters")]
    public async Task<ActionResult<List<WeakLetterDto>>> GetStudentWeakLetters(Guid studentId)
    {
        var records = await analyticsService.GetWeakLettersAsync(studentId);
        var dtos = records
            .Select(r => new WeakLetterDto(
                r.Letter, r.Attempts, r.Correct,
                r.Attempts > 0 ? Math.Round(r.Correct / (double)r.Attempts * 100, 1) : 0,
                r.ActivityType, r.LastSeenAt))
            .OrderBy(d => d.Accuracy)
            .ToList();
        return Ok(dtos);
    }

    // ── Class-level analytics for a teacher ───────────────────────────────────
    [HttpGet("teacher/{teacherId:guid}/class")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetClassAnalytics(Guid teacherId)
    {
        var summary = await analyticsService.GetClassAnalyticsAsync(teacherId);
        return Ok(summary);
    }

    // ── Record a writing result for analytics ─────────────────────────────────
    [HttpPost("record")]
    public async Task<IActionResult> RecordActivity([FromBody] RecordActivityRequest req)
    {
        await analyticsService.UpsertWeakLetterAsync(
            req.StudentId, req.ChildName, req.Letter, req.Correct, req.ActivityType);
        return Ok(new { message = "تم تسجيل النشاط." });
    }
}

public record RecordActivityRequest(
    Guid StudentId,
    string ChildName,
    string Letter,
    bool Correct,
    string ActivityType);
