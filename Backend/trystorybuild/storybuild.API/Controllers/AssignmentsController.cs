using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsController(
    ILessonAssignmentRepository assignmentRepository,
    IAssignmentSubmissionRepository submissionRepository,
    IStudentGroupRepository groupRepository) : ControllerBase
{
    // ── Student: get my assignments with submission status ─────────────────────
    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<AssignmentDto>>> GetStudentAssignments(Guid studentId)
    {
        var groups      = await groupRepository.GetGroupsForStudentAsync(studentId);
        var groupIds    = groups.Select(g => g.Id).ToList();
        var assignments = await assignmentRepository.GetForStudentAsync(studentId, groupIds);
        var submissions = await submissionRepository.GetByStudentAsync(studentId);
        var subMap      = submissions.ToDictionary(s => s.AssignmentId);

        var dtos = assignments.Select(a =>
        {
            subMap.TryGetValue(a.Id, out var sub);
            return new AssignmentDto(
                a.Id,
                a.LessonId,
                a.Lesson?.Title ?? "",
                a.Lesson?.Letter ?? "",
                a.Lesson?.Level ?? 0,
                a.TargetType,
                a.AssignedAt,
                sub is not null,
                sub?.WritingScore ?? 0,
                sub?.IsComplete ?? false);
        }).ToList();

        return Ok(dtos);
    }

    // ── Student: submit / update assignment progress ───────────────────────────
    [HttpPost("{assignmentId:guid}/submit")]
    public async Task<ActionResult<AssignmentSubmissionDto>> SubmitAssignment(
        Guid assignmentId,
        [FromBody] SubmitAssignmentRequest req)
    {
        var existing = await submissionRepository.GetByAssignmentAndStudentAsync(assignmentId, req.StudentId);

        if (existing is not null)
        {
            existing.PagesCompleted = req.PagesCompleted;
            existing.TotalPages     = req.TotalPages;
            existing.WritingScore   = req.WritingScore;
            existing.IsComplete     = req.IsComplete;
            existing.SubmittedAt    = DateTime.UtcNow;
            var updated = await submissionRepository.SaveAsync(existing);
            return Ok(ToDto(updated));
        }

        var sub = new AssignmentSubmission
        {
            AssignmentId    = assignmentId,
            StudentId       = req.StudentId,
            ChildName       = req.ChildName,
            PagesCompleted  = req.PagesCompleted,
            TotalPages      = req.TotalPages,
            WritingScore    = req.WritingScore,
            IsComplete      = req.IsComplete
        };
        var saved = await submissionRepository.SaveAsync(sub);
        return Ok(ToDto(saved));
    }

    // ── Teacher: view all submissions for an assignment ────────────────────────
    [HttpGet("{assignmentId:guid}/submissions")]
    public async Task<ActionResult<List<AssignmentSubmissionDto>>> GetSubmissions(Guid assignmentId)
    {
        var subs = await submissionRepository.GetByAssignmentAsync(assignmentId);
        return Ok(subs.Select(ToDto).ToList());
    }

    // ── Teacher: get assignments they created with submission counts ───────────
    [HttpGet("teacher/{teacherId:guid}/overview")]
    public async Task<IActionResult> GetTeacherOverview(Guid teacherId)
    {
        var assignments = await assignmentRepository.GetByTeacherAsync(teacherId);
        var result = new List<object>();
        foreach (var a in assignments)
        {
            var subs = await submissionRepository.GetByAssignmentAsync(a.Id);
            result.Add(new
            {
                assignmentId    = a.Id,
                lessonId        = a.LessonId,
                lessonTitle     = a.Lesson?.Title ?? "",
                letter          = a.Lesson?.Letter ?? "",
                level           = a.Lesson?.Level ?? 0,
                targetType      = a.TargetType,
                targetStudentId = a.TargetStudentId,
                targetGroupId   = a.TargetGroupId,
                assignedAt      = a.AssignedAt,
                submissionCount = subs.Count,
                completedCount  = subs.Count(s => s.IsComplete),
                avgScore        = subs.Count > 0 ? Math.Round(subs.Average(s => s.WritingScore), 1) : 0
            });
        }
        return Ok(result);
    }

    private static AssignmentSubmissionDto ToDto(AssignmentSubmission s) =>
        new(s.Id, s.AssignmentId, s.StudentId, s.ChildName,
            s.PagesCompleted, s.TotalPages, s.WritingScore, s.IsComplete, s.SubmittedAt);
}

public record SubmitAssignmentRequest(
    Guid StudentId,
    string ChildName,
    int PagesCompleted,
    int TotalPages,
    double WritingScore,
    bool IsComplete);
