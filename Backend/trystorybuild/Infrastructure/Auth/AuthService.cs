using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Auth
{
    public class AuthService(
        IUserRepository    userRepo,
        IStudentRepository studentRepo,
        IConfiguration     config,
        AppDbContext       db) : IAuthService
    {
        // ── Adult registration ──────────────────────────────────────────────────
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await userRepo.FindByEmailAsync(request.Email) is not null)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل.");

            var role = request.Role.ToLower() switch
            {
                "parent"  => UserRole.Parent,
                "teacher" => UserRole.Teacher,
                _         => throw new ArgumentException("دور غير صالح.")
            };

            var user = new User
            {
                Name         = request.FullName,
                Email        = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role         = role,
            };
            await userRepo.SaveAsync(user);

            if (role == UserRole.Parent)
                await userRepo.SaveParentAsync(new Parent { Id = user.Id });
            else
                await userRepo.SaveTeacherAsync(new Teacher
                {
                    Id             = user.Id,
                    IsPrivate      = !request.SchoolManagerId.HasValue,
                    SchoolManagerId = request.SchoolManagerId
                });

            return ToAuthResponse(user, request.SchoolManagerId);
        }

        // ── Adult login ─────────────────────────────────────────────────────────
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await userRepo.FindByEmailAsync(request.Email.Trim().ToLower())
                ?? throw new InvalidOperationException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidOperationException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            if (!user.IsActive || user.IsBlocked)
                throw new InvalidOperationException("الحساب موقوف. تواصل مع الدعم.");

            Guid? schoolManagerId = null;
            if (user.Role == UserRole.Teacher)
            {
                var teacher = await userRepo.GetTeacherByIdAsync(user.Id);
                schoolManagerId = teacher?.SchoolManagerId;
            }

            return ToAuthResponse(user, schoolManagerId);
        }

        // ── Create student (by parent/teacher) ──────────────────────────────────
        public async Task<StudentAuthResponse> CreateStudentAsync(Guid creatorId, CreateStudentRequest request)
        {
            var normalised   = request.Username.Trim().ToLower();
            var nationalId   = request.NationalId.Trim();

            if (string.IsNullOrWhiteSpace(nationalId))
                throw new InvalidOperationException("الرقم التعريفي للطفل مطلوب.");

            if (await studentRepo.FindByUsernameAsync(normalised) is not null)
                throw new InvalidOperationException("اسم المستخدم مستخدم بالفعل.");

            if (await studentRepo.FindByNationalIdAsync(nationalId) is not null)
                throw new InvalidOperationException("هذا الطفل مسجل بالفعل.");

            if (request.ImagePin1 < 1 || request.ImagePin1 > 20)
                throw new ArgumentException("رمز الصورة يجب أن يكون بين 1 و 20.");

            var creator = await userRepo.FindByIdAsync(creatorId)
                ?? throw new InvalidOperationException("المستخدم غير موجود.");

            // School teachers must belong to a classroom before adding students
            Guid? autoEnrollClassroomId = null;
            if (creator.Role == UserRole.Teacher)
            {
                var teacher = await userRepo.GetTeacherByIdAsync(creatorId);
                if (teacher?.SchoolManagerId.HasValue == true)
                {
                    var classroom = await db.Classrooms
                        .FirstOrDefaultAsync(c => c.TeacherId == creatorId);
                    if (classroom is null)
                        throw new InvalidOperationException("لم يتم تعيينك في أي فصل دراسي بعد. تواصل مع مدير المدرسة.");
                    autoEnrollClassroomId = classroom.Id;
                }
            }

            var student = new Student
            {
                Name         = request.Name,
                Age          = request.Age,
                Username     = normalised,
                NationalId   = nationalId,
                ImagePin1    = request.ImagePin1,
                ImagePin2    = request.ImagePin2,
                Level        = request.Level,
                AvatarEmoji  = request.AvatarEmoji,
                LoginMethod  = StudentLoginMethod.ImagePin,
                ParentId     = creator.Role == UserRole.Parent  ? creatorId : null,
                TeacherId    = creator.Role == UserRole.Teacher ? creatorId : null,
            };
            await studentRepo.SaveAsync(student);

            if (autoEnrollClassroomId.HasValue)
            {
                db.ClassroomStudents.Add(new ClassroomStudent
                {
                    ClassroomId = autoEnrollClassroomId.Value,
                    StudentId   = student.Id,
                });
                await db.SaveChangesAsync();
            }

            return ToStudentResponse(student);
        }

        // ── Student login ───────────────────────────────────────────────────────
        public async Task<StudentAuthResponse> StudentLoginAsync(StudentLoginRequest request)
        {
            var student = await studentRepo.FindByUsernameAsync(request.Username.Trim().ToLower())
                ?? throw new InvalidOperationException("اسم المستخدم غير موجود.");

            if (student.ImagePin1 != request.ImagePin1 || student.ImagePin2 != request.ImagePin2)
                throw new InvalidOperationException("رمز الصورة غير صحيح.");

            return ToStudentResponse(student);
        }

        // ── List children (for parent dashboard) ───────────────────────────────
        public async Task<List<StudentProfileDto>> GetChildrenAsync(Guid parentId)
        {
            var children = await studentRepo.GetByParentIdAsync(parentId);
            return children.Select(ToSummary).ToList();
        }

        // ── List students (for teacher dashboard) ───────────────────────────────
        public async Task<List<StudentProfileDto>> GetStudentsAsync(Guid teacherId)
        {
            var students = await studentRepo.GetByTeacherIdAsync(teacherId);
            return students.Select(ToSummary).ToList();
        }

        // ── Update student level after placement test ───────────────────────────
        public async Task<StudentAuthResponse> UpdateStudentLevelAsync(Guid studentId, int level)
        {
            var ok = await studentRepo.UpdateLevelAsync(studentId, level);
            if (!ok) throw new InvalidOperationException("الطالب غير موجود.");
            var student = await studentRepo.FindByIdAsync(studentId)
                ?? throw new InvalidOperationException("الطالب غير موجود.");
            return ToStudentResponse(student);
        }

        // ── Delete student (teacher or parent who owns the student) ────────────
        public async Task DeleteStudentAsync(Guid callerId, Guid studentId)
        {
            var student = await db.Students.FindAsync(studentId)
                ?? throw new KeyNotFoundException("الطالب غير موجود.");

            var caller = await db.Users.FindAsync(callerId)
                ?? throw new UnauthorizedAccessException();

            bool authorized = caller.Role switch
            {
                UserRole.Teacher     => student.TeacherId == callerId,
                UserRole.Parent      => student.ParentId  == callerId,
                UserRole.SystemAdmin => true,
                _                    => false,
            };
            if (!authorized)
                throw new UnauthorizedAccessException("غير مصرح لك بحذف هذا الطالب.");

            // Delete dependent rows before deleting the student
            await db.FluencyReports
                .Where(f => f.Recording.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.AudioRecordings
                .Where(a => a.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.WeakLetterRecords
                .Where(w => w.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.AssignmentSubmissions
                .Where(a => a.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.StudentLevelHistories
                .Where(h => h.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.WordJournalEntries
                .Where(e => e.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.Annotations
                .Where(a => a.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.LessonPageCompletions
                .Where(c => c.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.StudentContentCompletions
                .Where(c => c.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.LearningAttempts
                .Where(a => a.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.WritingAttempts
                .Where(w => w.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.StudentProgress
                .Where(p => p.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.StudentGroupMembers
                .Where(m => m.StudentId == studentId)
                .ExecuteDeleteAsync();
            await db.ClassroomStudents
                .Where(cs => cs.StudentId == studentId)
                .ExecuteDeleteAsync();

            db.Students.Remove(student);
            await db.SaveChangesAsync();
        }

        // ── Create school admin (system admin only) ─────────────────────────────
        public async Task<Guid> CreateSchoolAdminAsync(
            string schoolName, string email, string password)
        {
            var normalised = email.Trim().ToLower();
            if (await userRepo.FindByEmailAsync(normalised) is not null)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل.");

            var user = new User
            {
                Name         = schoolName.Trim(),
                Email        = normalised,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role         = UserRole.SchoolAdmin,
            };
            await userRepo.SaveAsync(user);
            return user.Id;
        }

        // ── Token generation ────────────────────────────────────────────────────
        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token  = new JwtSecurityToken(
                issuer:             config["Jwt:Issuer"],
                audience:           config["Jwt:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddDays(30),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private AuthResponse ToAuthResponse(User user, Guid? schoolManagerId = null)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name,             user.Name),
                new Claim(ClaimTypes.Email,            user.Email),
                new Claim(ClaimTypes.Role,             user.Role.ToString()),
            };
            var expiry = DateTime.UtcNow.AddDays(30);
            return new AuthResponse(
                GenerateToken(claims),
                user.Id.ToString(),
                user.Name,
                user.Role.ToString().ToLower(),
                expiry,
                schoolManagerId);
        }

        private StudentAuthResponse ToStudentResponse(Student student)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, student.Id.ToString()),
                new Claim(ClaimTypes.Name,             student.Name),
                new Claim(ClaimTypes.Role,             "Student"),
                new Claim("level",                     student.Level.ToString()),
            };
            var expiry = DateTime.UtcNow.AddDays(30);
            return new StudentAuthResponse(
                GenerateToken(claims),
                student.Id.ToString(),
                student.Name,
                student.Level,
                student.PlacementDone,
                expiry,
                student.AvatarEmoji);
        }

        private static StudentProfileDto ToSummary(Student s) =>
            new(s.Id, s.Name, s.Age, s.Username, s.NationalId, s.Level, s.PlacementDone, s.AvatarUrl, s.AvatarEmoji);
    }
}
