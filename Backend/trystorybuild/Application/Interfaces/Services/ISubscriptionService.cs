using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ISubscriptionService
    {
        /// <summary>
        /// Resolves the subscription that covers a student (via parent, private teacher,
        /// or school admin), then checks whether that plan allows the requested feature.
        /// Phase 1: always returns IsAllowed = true — enforcement added in Phase 2.
        /// </summary>
        Task<SubscriptionAccessResult> CheckAccessAsync(
            Guid studentId,
            SubscriptionFeature feature,
            Guid? contentId = null);

        /// <summary>
        /// Checks access for an adult user directly (for teacher/admin portals).
        /// </summary>
        Task<SubscriptionAccessResult> CheckUserAccessAsync(
            Guid userId,
            SubscriptionFeature feature);

        /// <summary>Returns the first active (non-expired) subscription for a user, or null.</summary>
        Task<Subscription?> GetActiveSubscriptionForUserAsync(Guid userId);

        /// <summary>
        /// Walks the student → parent/teacher → schoolAdmin chain and returns
        /// the subscription that governs this student's access.
        /// </summary>
        Task<Subscription?> ResolveSubscriptionForStudentAsync(Guid studentId);
    }
}
