using Application.DTOs;

namespace Application.Interfaces
{
    // ── Dashboard Service ──────────────────────────────────────────────────────
    public interface IDashboardService
    {
        Task<StudentDashboardDto?> GetStudentDashboardAsync(string childName);
        Task<ParentDashboardDto?>  GetParentDashboardAsync(string childName);
        Task<TeacherDashboardDto>  GetTeacherDashboardAsync(Guid teacherId);
        Task<SchoolDashboardDto>   GetSchoolDashboardAsync(string schoolCode);
        Task<List<string>>         GetKnownChildNamesAsync();
        Task<List<LevelProgressDto>> GetLevelProgressAsync(string childName);
    }
}
