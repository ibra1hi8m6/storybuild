using Domain.Entities;

namespace Application.Interfaces
{
    // ── Story Repository ───────────────────────────────────────────────────────
    public interface IStoryRepository
    {
        Task<Story> SaveAsync(Story story);
        Task<Story?> GetByIdAsync(Guid id);
        Task<List<Story>> GetAllAsync(bool publishedOnly = false);
        Task<List<Story>> GetByChildNameAsync(string childName);
        Task<List<Story>> GetByStudentIdAsync(Guid studentId);
        Task<bool> DeleteAsync(Guid id);
    }

    // ── Lesson Repository ──────────────────────────────────────────────────────
    public interface ILessonRepository
    {
        Task<Lesson> SaveAsync(Lesson lesson);
        Task<Lesson?> GetByIdAsync(Guid id);
        Task<List<Lesson>> GetByLevelAsync(int level, bool publishedOnly = true);
        Task<List<Lesson>> GetAllAsync(int? level = null, bool publishedOnly = false);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> UpdatePageSentenceAsync(Guid pageId, string sentence);
        Task<Lesson> CreateManualAsync(Lesson lesson);
    }

    // ── Exam Repository ────────────────────────────────────────────────────────
    public interface IExamRepository
    {
        Task<Exam> SaveAsync(Exam exam);
        Task<Exam?> GetByStoryIdAsync(Guid storyId);
        Task<Exam?> GetByLessonIdAsync(Guid lessonId);
        Task<Exam?> GetByIdAsync(Guid examId);
        Task SaveAnswersAsync(Guid examId, List<StudentAnswer> answers);
    }

    // ── Progress Repository ────────────────────────────────────────────────────
    public interface IStudentProgressRepository
    {
        Task<StudentProgress> SaveAsync(StudentProgress progress);
        Task<StudentProgress?> GetAsync(Guid storyId, string childName);
        Task<StudentProgress?> GetByStudentAsync(Guid storyId, Guid studentId);
        Task<StudentProgress?> GetByLessonAsync(Guid lessonId, string childName);
    }
}
