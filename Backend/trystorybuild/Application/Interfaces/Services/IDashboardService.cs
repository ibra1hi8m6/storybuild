using Application.DTOs;

namespace Application.Interfaces
{
    // ── Dashboard Service ──────────────────────────────────────────────────────
    public interface IDashboardService
    {
        Task<StudentDashboardDto?> GetStudentDashboardAsync(Guid studentId);
        Task<ParentDashboardDto?>  GetParentDashboardAsync(Guid studentId);
        Task<TeacherDashboardDto>  GetTeacherDashboardAsync(Guid teacherId);
        Task<SchoolDashboardDto>   GetSchoolDashboardAsync(Guid schoolManagerId);
        Task<List<string>>         GetKnownChildNamesAsync();
        Task<List<LevelProgressDto>> GetLevelProgressAsync(Guid studentId);
    }
}
