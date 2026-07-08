using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController(
    IStudentGroupRepository groupRepository,
    ILessonAssignmentRepository assignmentRepository,
    IStudentRepository studentRepository,
    AppDbContext db) : ControllerBase
{
    // ── Get teacher's groups ───────────────────────────────────────────────────
    [HttpGet("teacher/{teacherId:guid}")]
    public async Task<IActionResult> GetByTeacher(Guid teacherId)
    {
        var groups = await groupRepository.GetByTeacherIdAsync(teacherId);
        var dtos = groups.Select(g => new StudentGroupDto(
            g.Id, g.Name, g.TeacherId, g.Members.Count, g.CreatedAt,
            g.Members.Select(m => new StudentGroupMemberDto(
                m.StudentId, m.Student.Name, m.AddedAt)).ToList()
        )).ToList();
        return Ok(dtos);
    }

    // ── Create group ───────────────────────────────────────────────────────────
    [HttpPost("teacher/{teacherId:guid}")]
    public async Task<IActionResult> Create(Guid teacherId, [FromBody] CreateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "يرجى إدخال اسم المجموعة." });

        var isPrivate = await db.Teachers
            .Where(t => t.Id == teacherId)
            .Select(t => !t.SchoolManagerId.HasValue)
            .FirstOrDefaultAsync();

        if (isPrivate)
        {
            var plan = await GetActivePlanAsync(teacherId);
            if (plan != SubscriptionPlan.DemoFullAccess)
            {
                var maxGroups = plan == SubscriptionPlan.TeacherPremium
                    ? SubscriptionConstants.TeacherPremiumMaxGroups
                    : SubscriptionConstants.FreeTeacherMaxGroups;

                var groupCount = await db.Set<StudentGroup>().CountAsync(g => g.TeacherId == teacherId);
                if (groupCount >= maxGroups)
                    return StatusCode(403, new { message = "لقد وصلت إلى الحد الأقصى لعدد المجموعات في خطتك الحالية." });
            }
        }

        var group = new StudentGroup { Name = req.Name.Trim(), TeacherId = teacherId };
        var saved = await groupRepository.SaveAsync(group);
        return Ok(new StudentGroupDto(saved.Id, saved.Name, saved.TeacherId, 0, saved.CreatedAt, new()));
    }

    // ── Add member ─────────────────────────────────────────────────────────────
    [HttpPost("{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddGroupMemberRequest req)
    {
        var student = await studentRepository.FindByIdAsync(req.StudentId);
        if (student is null)
            return NotFound(new { error = "الطالب غير موجود." });

        var added = await groupRepository.AddMemberAsync(groupId, req.StudentId);
        return added ? Ok(new { message = "تمت الإضافة." }) : Conflict(new { error = "الطالب موجود بالفعل في المجموعة." });
    }

    // ── Remove member ──────────────────────────────────────────────────────────
    [HttpDelete("{groupId:guid}/members/{studentId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid studentId)
    {
        var removed = await groupRepository.RemoveMemberAsync(groupId, studentId);
        return removed ? Ok(new { message = "تمت الإزالة." }) : NotFound(new { error = "العضو غير موجود." });
    }

    // ── Delete group ───────────────────────────────────────────────────────────
    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        var deleted = await groupRepository.DeleteAsync(groupId);
        return deleted ? NoContent() : NotFound(new { error = "المجموعة غير موجودة." });
    }

    // ── Direct students (individual, no group) ─────────────────────────────────

    [HttpGet("teacher/{teacherId:guid}/direct-students")]
    public async Task<IActionResult> GetDirectStudents(Guid teacherId)
    {
        // Students already assigned to any group belonging to this teacher
        var groupedIds = await db.Set<StudentGroupMember>()
            .Where(m => m.Group.TeacherId == teacherId)
            .Select(m => m.StudentId)
            .ToListAsync();

        var students = await studentRepository.GetByTeacherIdAsync(teacherId);
        var direct   = students.Where(s => !groupedIds.Contains(s.Id));
        return Ok(direct.Select(s => new { id = s.Id, name = s.Name, level = s.Level }));
    }

    [HttpPost("teacher/{teacherId:guid}/direct-students")]
    public async Task<IActionResult> AddDirectStudent(Guid teacherId, [FromBody] AddDirectStudentRequest req)
    {
        var student = await studentRepository.FindByUsernameAsync(req.Identifier)
                   ?? await studentRepository.FindByNationalIdAsync(req.Identifier);
        if (student is null)
            return NotFound(new { error = "لم يتم العثور على طالب بهذا الاسم أو الرقم." });
        if (student.TeacherId.HasValue && student.TeacherId != teacherId)
            return Conflict(new { error = "هذا الطالب مرتبط بمعلم آخر بالفعل." });

        // Only check limit for new assignments (student not already assigned to this teacher)
        if (student.TeacherId != teacherId)
        {
            var isPrivate = await db.Teachers
                .Where(t => t.Id == teacherId)
                .Select(t => !t.SchoolManagerId.HasValue)
                .FirstOrDefaultAsync();

            if (isPrivate)
            {
                var plan = await GetActivePlanAsync(teacherId);
                if (plan != SubscriptionPlan.DemoFullAccess)
                {
                    var maxStudents = plan == SubscriptionPlan.TeacherPremium
                        ? SubscriptionConstants.TeacherPremiumMaxStudents
                        : SubscriptionConstants.FreeTeacherMaxStudents;

                    var count = await db.Students.CountAsync(s => s.TeacherId == teacherId);
                    if (count >= maxStudents)
                        return StatusCode(403, new { message = "لقد وصلت إلى الحد الأقصى لعدد الطلاب في خطتك الحالية." });
                }
            }
        }

        await studentRepository.SetTeacherAsync(student.Id, teacherId);
        return Ok(new { id = student.Id, name = student.Name, level = student.Level });
    }

    [HttpDelete("teacher/{teacherId:guid}/direct-students/{studentId:guid}")]
    public async Task<IActionResult> RemoveDirectStudent(Guid teacherId, Guid studentId)
    {
        var student = await studentRepository.FindByIdAsync(studentId);
        if (student is null || student.TeacherId != teacherId)
            return NotFound(new { error = "الطالب غير موجود في قائمتك المباشرة." });
        await studentRepository.SetTeacherAsync(studentId, null);
        return Ok(new { message = "تمت إزالة الطالب من قائمتك المباشرة." });
    }

    // ── Assign lesson to student or group ──────────────────────────────────────
    [HttpPost("assign")]
    [Authorize(Roles = "Teacher,SystemAdmin")]
    public async Task<IActionResult> AssignLesson([FromBody] AssignLessonRequest req)
    {
        if (req.TargetStudentId is null && req.TargetGroupId is null)
            return BadRequest(new { error = "يرجى تحديد طالب أو مجموعة." });

        var teacherIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(teacherIdStr, out var teacherId))
            return Unauthorized();

        var assignment = new LessonAssignment
        {
            LessonId        = req.LessonId,
            TeacherId       = teacherId,
            TargetType      = req.TargetType,
            TargetStudentId = req.TargetStudentId,
            TargetGroupId   = req.TargetGroupId
        };
        var saved = await assignmentRepository.SaveAsync(assignment);
        return Ok(new { id = saved.Id, message = "تم التعيين بنجاح." });
    }

    // ── Get assignments for teacher ────────────────────────────────────────────
    [HttpGet("assignments/teacher/{teacherId:guid}")]
    public async Task<IActionResult> GetTeacherAssignments(Guid teacherId)
    {
        var assignments = await assignmentRepository.GetByTeacherAsync(teacherId);
        var dtos = assignments.Select(a => new LessonAssignmentDto(
            a.Id, a.LessonId, a.Lesson?.Title ?? "",
            a.TargetType,
            a.TargetStudentId, null,
            a.TargetGroupId, null,
            a.AssignedAt)).ToList();
        return Ok(dtos);
    }

    // ── Get assigned lessons for student ──────────────────────────────────────
    [HttpGet("assigned/student/{studentId:guid}")]
    public async Task<IActionResult> GetStudentAssigned(Guid studentId)
    {
        var groups   = await groupRepository.GetGroupsForStudentAsync(studentId);
        var groupIds = groups.Select(g => g.Id).ToList();
        var assignments = await assignmentRepository.GetForStudentAsync(studentId, groupIds);

        var dtos = assignments.Select(a => new LessonAssignmentDto(
            a.Id, a.LessonId, a.Lesson?.Title ?? "",
            a.TargetType,
            a.TargetStudentId, null,
            a.TargetGroupId, null,
            a.AssignedAt)).ToList();
        return Ok(dtos);
    }

    private async Task<SubscriptionPlan?> GetActivePlanAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await db.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > now))
            .OrderByDescending(s => s.Plan)
            .Select(s => (SubscriptionPlan?)s.Plan)
            .FirstOrDefaultAsync();
    }
}
