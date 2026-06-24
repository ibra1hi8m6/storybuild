using Domain.Entities;

namespace Application.Interfaces
{
    // ── Writing Attempt Repository ─────────────────────────────────────────────
    public interface IWritingAttemptRepository
    {
        Task<WritingAttempt> SaveAsync(WritingAttempt attempt);
        Task<List<WritingAttempt>> GetByChildNameAsync(string childName, int take = 50);
        Task<int> CountByPageAsync(Guid pageId, string childName);
    }
}
