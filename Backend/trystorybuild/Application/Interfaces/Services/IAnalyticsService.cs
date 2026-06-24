using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    // ── Analytics Service ──────────────────────────────────────────────────────
    public interface IAnalyticsService
    {
        Task<List<WeakLetterRecord>> GetWeakLettersAsync(Guid studentId);
        Task UpsertWeakLetterAsync(Guid studentId, string childName, string letter, bool correct, string activityType);
        Task<AnalyticsSummaryDto> GetClassAnalyticsAsync(Guid teacherId);
    }
}
