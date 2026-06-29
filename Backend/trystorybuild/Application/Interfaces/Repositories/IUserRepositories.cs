using Domain.Entities;

namespace Application.Interfaces
{
    // ── User Repository ────────────────────────────────────────────────────────
    public interface IUserRepository
    {
        Task<User?>         FindByEmailAsync(string email);
        Task<User?>         FindByIdAsync(Guid id);
        Task<User>          SaveAsync(User user);
        Task<Parent>        SaveParentAsync(Parent parent);
        Task<Teacher>       SaveTeacherAsync(Teacher teacher);
        Task<Teacher?>      GetTeacherByIdAsync(Guid userId);
        Task<List<Teacher>> GetTeachersBySchoolManagerIdAsync(Guid schoolManagerId);
    }

    // ── Student Repository ─────────────────────────────────────────────────────
    public interface IStudentRepository
    {
        Task<Student?>       FindByUsernameAsync(string username);
        Task<Student?>       FindByNationalIdAsync(string nationalId);
        Task<Student?>       FindByIdAsync(Guid id);
        Task<Student>        SaveAsync(Student student);
        Task<List<Student>>  GetByParentIdAsync(Guid parentId);
        Task<List<Student>>  GetByTeacherIdAsync(Guid teacherId);
        Task<bool>           UpdateLevelAsync(Guid id, int level);
        Task<bool>           SetTeacherAsync(Guid studentId, Guid? teacherId);
    }
}
