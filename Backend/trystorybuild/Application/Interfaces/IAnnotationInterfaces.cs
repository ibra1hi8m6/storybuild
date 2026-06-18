using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAnnotationRepository
    {
        Task<Annotation> SaveAsync(Annotation annotation);
        Task<List<Annotation>> GetByPageAsync(Guid pageId, Guid studentId);
        Task<bool> DeleteAsync(Guid id, Guid studentId);
    }

    public interface IVocabularyRepository
    {
        Task<List<LessonVocabulary>> GetByLessonAsync(Guid lessonId);
        Task<LessonVocabulary> SaveAsync(LessonVocabulary vocab);
        Task<bool> DeleteAsync(Guid id);

        Task<WordJournalEntry> AddToJournalAsync(WordJournalEntry entry);
        Task<List<WordJournalEntry>> GetJournalAsync(Guid studentId);
        Task<bool> RemoveFromJournalAsync(Guid id, Guid studentId);
    }
}
