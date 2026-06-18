using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AnnotationRepository(AppDbContext db) : IAnnotationRepository
    {
        public async Task<Annotation> SaveAsync(Annotation annotation)
        {
            db.Annotations.Add(annotation);
            await db.SaveChangesAsync();
            return annotation;
        }

        public async Task<List<Annotation>> GetByPageAsync(Guid pageId, Guid studentId) =>
            await db.Annotations
                .Where(a => a.PageId == pageId && a.StudentId == studentId)
                .OrderBy(a => a.StartOffset)
                .ToListAsync();

        public async Task<bool> DeleteAsync(Guid id, Guid studentId)
        {
            var a = await db.Annotations.FirstOrDefaultAsync(x => x.Id == id && x.StudentId == studentId);
            if (a is null) return false;
            db.Annotations.Remove(a);
            await db.SaveChangesAsync();
            return true;
        }
    }

    public class VocabularyRepository(AppDbContext db) : IVocabularyRepository
    {
        public async Task<List<LessonVocabulary>> GetByLessonAsync(Guid lessonId) =>
            await db.LessonVocabularies
                .Where(v => v.LessonId == lessonId)
                .OrderBy(v => v.Order)
                .ToListAsync();

        public async Task<LessonVocabulary> SaveAsync(LessonVocabulary vocab)
        {
            db.LessonVocabularies.Add(vocab);
            await db.SaveChangesAsync();
            return vocab;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var v = await db.LessonVocabularies.FindAsync(id);
            if (v is null) return false;
            db.LessonVocabularies.Remove(v);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<WordJournalEntry> AddToJournalAsync(WordJournalEntry entry)
        {
            db.WordJournalEntries.Add(entry);
            await db.SaveChangesAsync();
            return entry;
        }

        public async Task<List<WordJournalEntry>> GetJournalAsync(Guid studentId) =>
            await db.WordJournalEntries
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        public async Task<bool> RemoveFromJournalAsync(Guid id, Guid studentId)
        {
            var e = await db.WordJournalEntries
                .FirstOrDefaultAsync(x => x.Id == id && x.StudentId == studentId);
            if (e is null) return false;
            db.WordJournalEntries.Remove(e);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
