using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        IAuthService authService,
        IUserRepository userRepository,
        IEmailService emailService,
        AppDbContext db) : ControllerBase
    {
        // ── Adult register ──────────────────────────────────────────────────────
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Teacher limit check (applies to all school plans)
                if (request.Role?.ToLower() == "teacher" && request.SchoolManagerId.HasValue)
                {
                    var schoolManagerId = request.SchoolManagerId.Value;
                    var now = DateTime.UtcNow;
                    var subscription = await db.Subscriptions
                        .Where(s => s.UserId == schoolManagerId && s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > now))
                        .OrderByDescending(s => s.Plan)
                        .FirstOrDefaultAsync();

                    var schoolPlan = subscription?.Plan;
                    if (schoolPlan != SubscriptionPlan.DemoFullAccess)
                    {
                        var maxTeachers = schoolPlan == SubscriptionPlan.SchoolPremium
                            ? (subscription!.MaxTeachers ?? SubscriptionConstants.SchoolPremiumDefaultMaxTeachers)
                            : SubscriptionConstants.FreeSchoolMaxTeachers;

                        var teacherCount = await db.Teachers.CountAsync(t => t.SchoolManagerId == schoolManagerId);
                        if (teacherCount >= maxTeachers)
                            return StatusCode(403, new { message = "لقد وصلت إلى الحد الأقصى لعدد المعلمين في خطتك الحالية." });
                    }
                }

                var result = await authService.RegisterAsync(request);
                // If a school admin creates a teacher, send welcome email with credentials
                if (request.Role?.ToLower() == "teacher" && request.SchoolManagerId.HasValue)
                    await emailService.SendTeacherWelcomeAsync(request.Email, request.FullName, request.Password);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
            catch (ArgumentException         ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Adult login ─────────────────────────────────────────────────────────
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try   { return Ok(await authService.LoginAsync(request)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        // ── Create student profile (parent or teacher only) ─────────────────────
        [HttpPost("students")]
        [Authorize(Roles = "Parent,Teacher")]
        [ProducesResponseType(typeof(StudentAuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
        {
            var creatorId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            try   { return Ok(await authService.CreateStudentAsync(creatorId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Student login (username + image PIN) ────────────────────────────────
        [HttpPost("students/login")]
        [ProducesResponseType(typeof(StudentAuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> StudentLogin([FromBody] StudentLoginRequest request)
        {
            try   { return Ok(await authService.StudentLoginAsync(request)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        // ── List children for logged-in parent ──────────────────────────────────
        [HttpGet("students")]
        [Authorize(Roles = "Parent,Teacher")]
        [ProducesResponseType(typeof(List<StudentProfileDto>), 200)]
        public async Task<IActionResult> GetStudents()
        {
            var userId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var list = role == "Parent"
                ? await authService.GetChildrenAsync(userId)
                : await authService.GetStudentsAsync(userId);

            return Ok(list);
        }

        // ── Current user profile ────────────────────────────────────────────────
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(200)]
        public IActionResult Me()
        {
            var id   = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return Ok(new { id, name, role });
        }

        // ── Delete student (teacher or parent who owns the student) ──────────────
        [HttpDelete("students/{id:guid}")]
        [Authorize(Roles = "Teacher,Parent,SystemAdmin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            var callerId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            try
            {
                await authService.DeleteStudentAsync(callerId, id);
                return NoContent();
            }
            catch (KeyNotFoundException    ex) { return NotFound(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ── Teacher adjusts a specific student's level ────────────────────────
        [HttpPatch("students/{id:guid}/level")]
        [Authorize(Roles = "Teacher,SystemAdmin")]
        [ProducesResponseType(typeof(StudentAuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateChildLevel(Guid id, [FromBody] UpdateLevelRequest request)
        {
            if (request.Level < 1 || request.Level > 3)
                return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 3." });

            try   { return Ok(await authService.UpdateStudentLevelAsync(id, request.Level)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Update student level after placement test ───────────────────────────
        [HttpPatch("students/me/level")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(StudentAuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateMyLevel([FromBody] UpdateLevelRequest request)
        {
            if (request.Level < 1 || request.Level > 3)
                return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 3." });

            var studentId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            try   { return Ok(await authService.UpdateStudentLevelAsync(studentId, request.Level)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── School admin: reset a teacher's password ────────────────────────────
        [HttpPost("school/teachers/{teacherId:guid}/reset-password")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> ResetTeacherPassword(Guid teacherId, [FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
                return BadRequest(new { error = "كلمة المرور يجب أن تكون 6 أحرف على الأقل." });

            var adminId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            // Verify teacher belongs to this school
            var teacher = await db.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == teacherId && t.SchoolManagerId == adminId);
            if (teacher is null) return NotFound(new { error = "المعلم غير موجود أو لا ينتمي لمدرستك." });

            teacher.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();

            await emailService.SendTeacherPasswordResetAsync(teacher.User.Email, teacher.User.Name, req.NewPassword);
            return Ok(new { message = "تم إعادة تعيين كلمة المرور وإرسالها للمعلم بالبريد الإلكتروني." });
        }

        // ── School admin: list teachers belonging to this school ─────────────────
        [HttpGet("school/teachers")]
        [Authorize(Roles = "SchoolAdmin")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSchoolTeachers()
        {
            var adminId    = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            var teachers = await userRepository.GetTeachersBySchoolManagerIdAsync(adminId);

            var teacherIds = teachers.Select(t => t.Id).ToList();

            // Step 1: classrooms for these teachers
            var classrooms = await db.Classrooms
                .Where(c => teacherIds.Contains(c.TeacherId))
                .Select(c => new { c.Id, c.TeacherId })
                .ToListAsync();

            // Step 2: student counts per classroom
            var classroomIds = classrooms.Select(c => c.Id).ToList();
            var countsByClassroom = await db.ClassroomStudents
                .Where(cs => classroomIds.Contains(cs.ClassroomId))
                .GroupBy(cs => cs.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
                .ToListAsync();

            var countByClassroom = countsByClassroom.ToDictionary(x => x.ClassroomId, x => x.Count);
            var countByTeacher   = classrooms
                .GroupBy(c => c.TeacherId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(c => countByClassroom.GetValueOrDefault(c.Id, 0)));

            var result = teachers.Select(t => new
            {
                id           = t.Id,
                name         = t.User?.Name ?? "",
                email        = t.User?.Email ?? "",
                studentCount = countByTeacher.GetValueOrDefault(t.Id, 0),
            });

            return Ok(result);
        }
    }

    public record UpdateLevelRequest(int Level);
    public record ResetPasswordRequest(string NewPassword);
}
