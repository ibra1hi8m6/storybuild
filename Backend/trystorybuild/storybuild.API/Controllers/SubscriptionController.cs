using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionController(
    ISubscriptionService subscriptionService,
    AppDbContext db) : ControllerBase
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Invalid token."));

    private string CurrentRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    // ── GET /api/subscriptions/me ──────────────────────────────────────────────

    /// <summary>Returns the active subscription + usage counts for the calling adult user.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Parent,Teacher,SchoolAdmin,SystemAdmin")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = CurrentUserId();
        var user   = await db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { error = "User not found." });

        var sub  = await subscriptionService.GetActiveSubscriptionForUserAsync(userId);
        var plan = sub?.Plan;

        // ── Usage counts per role ───────────────────────────────────────────
        int? childrenCount = null, maxChildren  = null;
        int? studentsCount = null, maxStudents  = null;
        int? groupsCount   = null, maxGroups    = null;
        int? classesCount  = null, maxClasses   = null;
        int? teachersCount = null, maxTeachers  = null;
        bool isSchoolTeacher     = false;
        bool inheritedFromSchool = false;
        int? maxStudentsPerClass = null;

        bool isFreePlan = plan is null
            or SubscriptionPlan.Free
            or SubscriptionPlan.TeacherFree
            or SubscriptionPlan.SchoolTrial;

        if (user.Role == UserRole.Parent)
        {
            childrenCount = await db.Students.CountAsync(s => s.ParentId == userId);
            maxChildren   = plan == SubscriptionPlan.ParentPremium
                ? SubscriptionConstants.ParentPremiumMaxChildren
                : SubscriptionConstants.FreeParentMaxChildren;
        }
        else if (user.Role == UserRole.Teacher)
        {
            var teacherEntity = await db.Teachers
                .Where(t => t.Id == userId)
                .Select(t => new { t.SchoolManagerId })
                .FirstOrDefaultAsync();

            if (teacherEntity?.SchoolManagerId is not null)
            {
                var schoolSub = await subscriptionService.GetActiveSubscriptionForUserAsync(teacherEntity.SchoolManagerId.Value);
                sub  = schoolSub;
                plan = schoolSub?.Plan;

                isSchoolTeacher     = true;
                inheritedFromSchool = true;
                maxStudentsPerClass = SubscriptionConstants.SchoolPremiumMaxStudentsPerClass;
                studentsCount       = await db.Students.CountAsync(s => s.TeacherId == userId);
            }
            else
            {
                studentsCount = await db.Students.CountAsync(s => s.TeacherId == userId);
                groupsCount   = await db.StudentGroups.CountAsync(g => g.TeacherId == userId);
                maxStudents   = plan == SubscriptionPlan.TeacherPremium
                    ? SubscriptionConstants.TeacherPremiumMaxStudents
                    : SubscriptionConstants.FreeTeacherMaxStudents;
                maxGroups     = plan == SubscriptionPlan.TeacherPremium
                    ? SubscriptionConstants.TeacherPremiumMaxGroups
                    : SubscriptionConstants.FreeTeacherMaxGroups;
            }
        }
        else if (user.Role == UserRole.SchoolAdmin)
        {
            classesCount  = await db.Classrooms.CountAsync(c => c.SchoolManagerId == userId);
            teachersCount = await db.Teachers.CountAsync(t => t.SchoolManagerId == userId);
            maxClasses    = plan == SubscriptionPlan.SchoolPremium
                ? SubscriptionConstants.SchoolPremiumMaxClasses
                : SubscriptionConstants.FreeSchoolMaxClasses;
            maxTeachers   = plan == SubscriptionPlan.SchoolPremium
                ? (sub!.MaxTeachers ?? SubscriptionConstants.SchoolPremiumDefaultMaxTeachers)
                : SubscriptionConstants.FreeSchoolMaxTeachers;
        }

        return Ok(new
        {
            userId              = user.Id,
            role                = user.Role.ToString(),
            isDemo              = user.IsDemo,
            activePlan          = sub?.Plan.ToString() ?? "Free",
            expiresAt           = sub?.ExpiresAt,
            isActive            = sub?.IsActive ?? false,
            isSchoolTeacher,
            inheritedFromSchool,
            maxStudentsPerClass,
            childrenCount,
            maxChildren,
            studentsCount,
            maxStudents,
            groupsCount,
            maxGroups,
            classesCount,
            maxClasses,
            teachersCount,
            maxTeachers,
        });
    }

    // ── GET /api/subscriptions/student/{studentId}/check ──────────────────────

    /// <summary>
    /// Resolves and returns the SubscriptionAccessResult for a specific student + feature.
    /// Query param: ?feature=WritingEvaluation (name of SubscriptionFeature enum member).
    /// </summary>
    [HttpGet("student/{studentId:guid}/check")]
    [Authorize(Roles = "Parent,Teacher,SchoolAdmin,SystemAdmin")]
    public async Task<IActionResult> CheckStudentAccess(
        Guid studentId,
        [FromQuery] string feature = "Letters")
    {
        // Parse feature enum
        if (!Enum.TryParse<SubscriptionFeature>(feature, ignoreCase: true, out var parsedFeature))
            return BadRequest(new
            {
                error   = $"Unknown feature '{feature}'.",
                valid   = Enum.GetNames<SubscriptionFeature>(),
            });

        // Ownership check
        var authResult = await AuthorizeStudentAccessAsync(studentId);
        if (authResult is not null) return authResult;

        var result = await subscriptionService.CheckAccessAsync(studentId, parsedFeature);
        return Ok(result);
    }

    // ── GET /api/subscriptions/plans ──────────────────────────────────────────

    /// <summary>Returns all subscription plan names and feature names (enum reference).</summary>
    [HttpGet("plans")]
    public IActionResult GetPlans()
    {
        return Ok(new
        {
            plans    = Enum.GetNames<SubscriptionPlan>(),
            features = Enum.GetNames<SubscriptionFeature>(),
        });
    }

    // ── POST /api/subscriptions/activate ──────────────────────────────────────

    /// <summary>Activate a paid subscription using an activation code.</summary>
    [HttpPost("activate")]
    [Authorize(Roles = "Parent,Teacher,SchoolAdmin,SystemAdmin")]
    public async Task<IActionResult> Activate([FromBody] ActivateSubscriptionRequest req)
    {
        var userId = CurrentUserId();
        var role   = CurrentRole();
        var now    = DateTime.UtcNow;

        var normalised = (req.Code ?? "").Trim().ToUpper();
        if (string.IsNullOrEmpty(normalised))
            return BadRequest(new { error = "الكود مطلوب." });

        var code = await db.SubscriptionActivationCodes
            .FirstOrDefaultAsync(c => c.Code == normalised);

        if (code is null)
            return BadRequest(new { error = "الكود غير موجود." });

        if (!code.IsActive)
            return BadRequest(new { error = "الكود غير فعّال." });

        if (code.ExpiresAt.HasValue && code.ExpiresAt < now)
            return BadRequest(new { error = "الكود منتهي الصلاحية." });

        if (code.UsedCount >= code.MaxUses)
            return BadRequest(new { error = "تم استخدام الكود بالكامل." });

        // Role → plan validation (SystemAdmin may activate any plan)
        if (role != nameof(UserRole.SystemAdmin))
        {
            var allowed = role switch
            {
                nameof(UserRole.Parent)     => SubscriptionPlan.ParentPremium,
                nameof(UserRole.Teacher)    => SubscriptionPlan.TeacherPremium,
                nameof(UserRole.SchoolAdmin)=> SubscriptionPlan.SchoolPremium,
                _                           => (SubscriptionPlan?)null,
            };
            if (allowed is null || code.Plan != allowed)
                return BadRequest(new { error = "هذه الخطة غير متاحة لهذا النوع من الحساب." });
        }

        // Deactivate existing subscriptions
        var existing = await db.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();
        foreach (var sub in existing)
            sub.IsActive = false;

        // Create new subscription
        var newSub = new Subscription
        {
            UserId    = userId,
            Plan      = code.Plan,
            StartsAt  = now,
            ExpiresAt = now.AddDays(code.DurationDays),
            IsActive  = true,
        };
        db.Subscriptions.Add(newSub);

        code.UsedCount++;
        await db.SaveChangesAsync();

        return Ok(new
        {
            message   = "تم تفعيل الاشتراك بنجاح.",
            plan      = newSub.Plan.ToString(),
            expiresAt = newSub.ExpiresAt,
        });
    }

    // ── POST /api/subscriptions/codes  (SystemAdmin) ──────────────────────────

    [HttpPost("codes")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> CreateCode([FromBody] CreateActivationCodeRequest req)
    {
        if (!Enum.TryParse<SubscriptionPlan>(req.Plan, ignoreCase: true, out var plan))
            return BadRequest(new { error = $"خطة غير معروفة: {req.Plan}" });

        if (plan is SubscriptionPlan.Free or SubscriptionPlan.TeacherFree
                 or SubscriptionPlan.SchoolTrial or SubscriptionPlan.DemoFullAccess)
            return BadRequest(new { error = "لا يمكن إنشاء كود تفعيل لهذه الخطة." });

        if (req.DurationDays <= 0)
            return BadRequest(new { error = "مدة الاشتراك يجب أن تكون أكبر من 0 يوم." });

        if (req.MaxUses <= 0)
            return BadRequest(new { error = "عدد الاستخدامات يجب أن يكون أكبر من 0." });

        string codeStr;
        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            codeStr = req.Code.Trim().ToUpper();
            if (await db.SubscriptionActivationCodes.AnyAsync(c => c.Code == codeStr))
                return Conflict(new { error = "الكود موجود بالفعل." });
        }
        else
        {
            do { codeStr = GenerateCode(); }
            while (await db.SubscriptionActivationCodes.AnyAsync(c => c.Code == codeStr));
        }

        var entity = new SubscriptionActivationCode
        {
            Code            = codeStr,
            Plan            = plan,
            DurationDays    = req.DurationDays,
            MaxUses         = req.MaxUses,
            ExpiresAt       = req.ExpiresAt,
            Notes           = req.Notes?.Trim(),
            CreatedByUserId = CurrentUserId(),
        };
        db.SubscriptionActivationCodes.Add(entity);
        await db.SaveChangesAsync();

        return Ok(new
        {
            id           = entity.Id,
            code         = entity.Code,
            plan         = entity.Plan.ToString(),
            durationDays = entity.DurationDays,
            maxUses      = entity.MaxUses,
            usedCount    = entity.UsedCount,
            isActive     = entity.IsActive,
            expiresAt    = entity.ExpiresAt,
            notes        = entity.Notes,
            createdAt    = entity.CreatedAt,
        });
    }

    // ── GET /api/subscriptions/codes  (SystemAdmin) ───────────────────────────

    [HttpGet("codes")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> GetCodes()
    {
        var codes = await db.SubscriptionActivationCodes
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                id           = c.Id,
                code         = c.Code,
                plan         = c.Plan.ToString(),
                durationDays = c.DurationDays,
                maxUses      = c.MaxUses,
                usedCount    = c.UsedCount,
                isActive     = c.IsActive,
                expiresAt    = c.ExpiresAt,
                notes        = c.Notes,
                createdAt    = c.CreatedAt,
            })
            .ToListAsync();
        return Ok(codes);
    }

    // ── PATCH /api/subscriptions/codes/{id}/deactivate  (SystemAdmin) ─────────

    [HttpPatch("codes/{id:guid}/deactivate")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DeactivateCode(Guid id)
    {
        var code = await db.SubscriptionActivationCodes.FindAsync(id);
        if (code is null) return NotFound(new { error = "الكود غير موجود." });
        code.IsActive = false;
        await db.SaveChangesAsync();
        return Ok(new { message = "تم تعطيل الكود." });
    }

    // ── DELETE /api/subscriptions/codes/{id}  (SystemAdmin) ──────────────────

    [HttpDelete("codes/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DeleteCode(Guid id)
    {
        var code = await db.SubscriptionActivationCodes.FindAsync(id);
        if (code is null) return NotFound(new { error = "الكود غير موجود." });
        db.SubscriptionActivationCodes.Remove(code);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GenerateCode() =>
        Guid.NewGuid().ToString("N").ToUpper()[..12];

    // ── Ownership guard ────────────────────────────────────────────────────────

    private async Task<IActionResult?> AuthorizeStudentAccessAsync(Guid studentId)
    {
        var callerId = CurrentUserId();
        var role     = CurrentRole();

        if (role == nameof(UserRole.SystemAdmin))
            return null;

        var student = await db.Students.FindAsync(studentId);
        if (student is null)
            return NotFound(new { error = "Student not found." });

        switch (role)
        {
            case nameof(UserRole.Parent):
                if (student.ParentId != callerId)
                    return Forbid();
                break;

            case nameof(UserRole.Teacher):
                if (student.TeacherId != callerId)
                    return Forbid();
                break;

            case nameof(UserRole.SchoolAdmin):
                if (!student.TeacherId.HasValue)
                    return Forbid();

                var teacher = await db.Teachers.FindAsync(student.TeacherId.Value);
                if (teacher?.SchoolManagerId != callerId)
                    return Forbid();
                break;

            default:
                return Forbid();
        }

        return null;
    }
}
