using Domain.Entities;

namespace Application.Interfaces
{
    // ── Student Groups ─────────────────────────────────────────────────────────
    public interface IStudentGroupRepository
    {
        Task<StudentGroup> SaveAsync(StudentGroup group);
        Task<StudentGroup?> GetByIdAsync(Guid id);
        Task<List<StudentGroup>> GetByTeacherIdAsync(Guid teacherId);
        Task<bool> AddMemberAsync(Guid groupId, Guid studentId);
        Task<bool> RemoveMemberAsync(Guid groupId, Guid studentId);
        Task<bool> DeleteAsync(Guid id);
        Task<List<StudentGroup>> GetGroupsForStudentAsync(Guid studentId);
    }

    // ── Lesson Assignments ─────────────────────────────────────────────────────
    public interface ILessonAssignmentRepository
    {
        Task<LessonAssignment> SaveAsync(LessonAssignment assignment);
        Task<List<LessonAssignment>> GetForStudentAsync(Guid studentId, List<Guid> groupIds);
        Task<List<LessonAssignment>> GetByTeacherAsync(Guid teacherId);
    }

    // ── Assignment Submissions ─────────────────────────────────────────────────
    public interface IAssignmentSubmissionRepository
    {
        Task<AssignmentSubmission> SaveAsync(AssignmentSubmission submission);
        Task<AssignmentSubmission?> GetByAssignmentAndStudentAsync(Guid assignmentId, Guid studentId);
        Task<List<AssignmentSubmission>> GetByAssignmentAsync(Guid assignmentId);
        Task<List<AssignmentSubmission>> GetByStudentAsync(Guid studentId);
    }
}
