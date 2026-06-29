using Application.DTOs;

namespace Application.Interfaces
{
    // ── Auth Service ───────────────────────────────────────────────────────────
    public interface IAuthService
    {
        Task<AuthResponse>           RegisterAsync(RegisterRequest request);
        Task<AuthResponse>           LoginAsync(LoginRequest request);
        Task<StudentAuthResponse>    CreateStudentAsync(Guid creatorId, CreateStudentRequest request);
        Task<StudentAuthResponse>    StudentLoginAsync(StudentLoginRequest request);
        Task<List<StudentProfileDto>> GetChildrenAsync(Guid parentId);
        Task<List<StudentProfileDto>> GetStudentsAsync(Guid teacherId);
        Task<Guid> CreateSchoolAdminAsync(string schoolName, string email, string password);
        Task<StudentAuthResponse>    UpdateStudentLevelAsync(Guid studentId, int level);
        Task                         DeleteStudentAsync(Guid callerId, Guid studentId);
    }
}
