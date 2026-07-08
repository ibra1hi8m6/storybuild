using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Phase 1.6 — Real access decisions based on plan, feature, contentId, and attempt counts.
    /// IsAllowed is computed accurately but nothing enforces it yet (Phase 2 wires it into controllers).
    /// </summary>
    public class SubscriptionService(
        AppDbContext db,
        ILogger<SubscriptionService> logger) : ISubscriptionService
    {
        // ── Public interface ──────────────────────────────────────────────────────

        public async Task<SubscriptionAccessResult> CheckAccessAsync(
            Guid studentId,
            SubscriptionFeature feature,
            Guid? contentId = null)
        {
            var sub = await ResolveSubscriptionForStudentAsync(studentId);
            return await EvaluateAccessAsync(studentId, sub, feature, contentId);
        }

        public async Task<SubscriptionAccessResult> CheckUserAccessAsync(
            Guid userId,
            SubscriptionFeature feature)
        {
            var user = await db.Users.FindAsync(userId);
            if (user is not null && (user.Role == UserRole.SystemAdmin || user.IsDemo))
                return Allow("DemoFullAccess", isDemo: true, isFree: false);

            var sub = await GetActiveSubscriptionForUserAsync(userId);
            // No studentId context → can't count per-student attempts; evaluate plan tier only
            return await EvaluateAccessAsync(null, sub, feature, null);
        }

        public async Task<Subscription?> GetActiveSubscriptionForUserAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            return await db.Subscriptions
                .Where(s => s.UserId   == userId
                         && s.IsActive == true
                         && (s.ExpiresAt == null || s.ExpiresAt > now))
                .OrderByDescending(s => s.Plan)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription?> ResolveSubscriptionForStudentAsync(Guid studentId)
        {
            var student = await db.Students.FindAsync(studentId);
            if (student is null)
            {
                logger.LogWarning("[Subscription] Student {Id} not found.", studentId);
                return null;
            }

            if (student.ParentId.HasValue)
            {
                var sub = await GetActiveSubscriptionForUserAsync(student.ParentId.Value);
                logger.LogDebug("[Subscription] Student {S} → parent {P} → {Plan}", studentId, student.ParentId, sub?.Plan);
                return sub;
            }

            if (student.TeacherId.HasValue)
            {
                var teacher = await db.Teachers.FindAsync(student.TeacherId.Value);
                if (teacher is null) return null;

                if (teacher.SchoolManagerId.HasValue)
                {
                    var sub = await GetActiveSubscriptionForUserAsync(teacher.SchoolManagerId.Value);
                    logger.LogDebug("[Subscription] Student {S} → school admin {A} → {Plan}", studentId, teacher.SchoolManagerId, sub?.Plan);
                    return sub;
                }

                var privSub = await GetActiveSubscriptionForUserAsync(student.TeacherId.Value);
                logger.LogDebug("[Subscription] Student {S} → private teacher {T} → {Plan}", studentId, student.TeacherId, privSub?.Plan);
                return privSub;
            }

            logger.LogWarning("[Subscription] Student {Id} has no parent or teacher.", studentId);
            return null;
        }

        // ── Core evaluator ────────────────────────────────────────────────────────

        private async Task<SubscriptionAccessResult> EvaluateAccessAsync(
            Guid? studentId,
            Subscription? sub,
            SubscriptionFeature feature,
            Guid? contentId)
        {
            var plan   = sub?.Plan;
            var isDemo = plan == SubscriptionPlan.DemoFullAccess;
            var isFree = IsFreeTierPlan(plan);
            var planName = plan?.ToString() ?? "Free";

            // Premium + demo: allow everything unconditionally
            if (!isFree)
                return Allow(planName, isDemo, isFree: false);

            // ── Free-tier per-feature rules ───────────────────────────────────────
            switch (feature)
            {
                case SubscriptionFeature.Letters:
                    return await CheckContentInSetAsync(
                        contentId, GetFreeLetterIdsAsync, planName,
                        "هذا الحرف خارج نطاق الخطة المجانية. الأحرف المتاحة مجانًا هي الحروف الثلاثة الأولى فقط.");

                case SubscriptionFeature.Words:
                    return await CheckContentInSetAsync(
                        contentId, GetFreeWordIdsAsync, planName,
                        "هذه الكلمة خارج نطاق الخطة المجانية. الكلمات المتاحة مجانًا هي الكلمات الثلاث الأولى فقط.");

                case SubscriptionFeature.Sentences:
                    return await CheckContentInSetAsync(
                        contentId, GetFreeSentenceIdsAsync, planName,
                        "هذه الجملة خارج نطاق الخطة المجانية. الجمل المتاحة مجانًا هي الجمل الثلاث الأولى فقط.");

                case SubscriptionFeature.Booklets:
                    return await CheckBookletAccessAsync(contentId, planName);

                case SubscriptionFeature.Stories:
                    return await CheckStoryAccessAsync(contentId, planName);

                case SubscriptionFeature.AiStoryGeneration:
                    return await CheckAiStoryCountAsync(studentId, planName);

                case SubscriptionFeature.WritingEvaluation:
                    return await CheckAttemptLimitAsync(
                        studentId, contentId,
                        (sid, cid) => CountWritingAttemptsAsync(sid, cid),
                        SubscriptionConstants.FreeMaxAttempts, planName,
                        "لقد وصلت إلى الحد الأقصى من محاولات الكتابة المجانية لهذا المحتوى.");

                case SubscriptionFeature.ReadingFluency:
                    return await CheckAttemptLimitAsync(
                        studentId, contentId,
                        (sid, cid) => CountFluencyAttemptsAsync(sid, cid),
                        SubscriptionConstants.FreeMaxAttempts, planName,
                        "لقد وصلت إلى الحد الأقصى من محاولات القراءة المجانية لهذا المحتوى.");

                case SubscriptionFeature.TtsGeneration:
                    // TTS audio is cached globally — free users benefit from the shared cache.
                    // Gating new generations per user requires text context; deferred to Phase 2.
                    return Allow(planName, isDemo: false, isFree: true);

                case SubscriptionFeature.Exams:
                    return await CheckExamAccessAsync(contentId, planName);

                case SubscriptionFeature.Reports:
                    return Deny(planName, isFree: true,
                        "التقارير الكاملة متاحة للمشتركين المدفوعين فقط. يمكنك الاطلاع على ملخص أساسي في لوحة التحكم.", null);

                default:
                    return Allow(planName, isDemo: false, isFree: true);
            }
        }

        // ── Feature-specific checks ───────────────────────────────────────────────

        /// <summary>
        /// Checks whether contentId is within the free-tier allowed set.
        /// If contentId is null (just checking access in general), allow — browsing is not gated.
        /// </summary>
        private async Task<SubscriptionAccessResult> CheckContentInSetAsync(
            Guid? contentId,
            Func<Task<HashSet<Guid>>> getFreeIds,
            string planName,
            string denyReason)
        {
            if (contentId is null)
                return Allow(planName, isDemo: false, isFree: true);

            var freeIds = await getFreeIds();
            return freeIds.Contains(contentId.Value)
                ? Allow(planName, isDemo: false, isFree: true)
                : Deny(planName, isFree: true, denyReason, null);
        }

        private async Task<SubscriptionAccessResult> CheckBookletAccessAsync(Guid? contentId, string planName)
        {
            if (contentId is null)
                return Allow(planName, isDemo: false, isFree: true);

            var lesson = await db.Lessons
                .Where(l => l.Id == contentId)
                .Select(l => new { l.Id, l.Level })
                .FirstOrDefaultAsync();

            if (lesson is null)
                return Allow(planName, isDemo: false, isFree: true); // unknown booklet — allow gracefully

            var freeBookletId = await db.Lessons
                .Where(l => l.IsPublished && l.Level == lesson.Level)
                .OrderBy(l => l.CreatedAt)
                .Select(l => (Guid?)l.Id)
                .FirstOrDefaultAsync();

            if (freeBookletId is null)
                return Allow(planName, isDemo: false, isFree: true); // no booklets in this level yet

            return contentId == freeBookletId
                ? Allow(planName, isDemo: false, isFree: true)
                : Deny(planName, isFree: true,
                    "هذا الكتيب خارج نطاق الخطة المجانية. يمكنك الوصول إلى الكتيب الأول فقط مجانًا.", null);
        }

        private async Task<SubscriptionAccessResult> CheckStoryAccessAsync(Guid? contentId, string planName)
        {
            if (contentId is null)
                return Allow(planName, isDemo: false, isFree: true);

            var freeStoryId = await db.Stories
                .Where(s => s.IsPublished && s.Source == StorySource.PdfImport)
                .OrderBy(s => s.CreatedAt)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();

            if (freeStoryId is null)
                return Allow(planName, isDemo: false, isFree: true); // no PDF stories yet

            return contentId == freeStoryId
                ? Allow(planName, isDemo: false, isFree: true)
                : Deny(planName, isFree: true,
                    "هذه القصة خارج نطاق الخطة المجانية. يمكنك قراءة القصة الأولى فقط مجانًا.", null);
        }

        private async Task<SubscriptionAccessResult> CheckAiStoryCountAsync(Guid? studentId, string planName)
        {
            if (studentId is null)
                return Allow(planName, isDemo: false, isFree: true); // can't count without student context

            var count = await db.Stories
                .CountAsync(s => s.StudentId == studentId && s.Source == StorySource.AiGenerated);

            var remaining = Math.Max(0, SubscriptionConstants.FreeAiStoriesLimit - count);

            return remaining > 0
                ? Allow(planName, isDemo: false, isFree: true, remainingAttempts: remaining)
                : Deny(planName, isFree: true,
                    "لقد استخدمت حصتك المجانية من القصص المولّدة بالذكاء الاصطناعي.", 0);
        }

        private async Task<SubscriptionAccessResult> CheckAttemptLimitAsync(
            Guid? studentId,
            Guid? contentId,
            Func<Guid, Guid, Task<int>> countAttempts,
            int maxAttempts,
            string planName,
            string denyReason)
        {
            // Can't count without both student and content context — allow gracefully
            if (studentId is null || contentId is null)
                return Allow(planName, isDemo: false, isFree: true);

            var count     = await countAttempts(studentId.Value, contentId.Value);
            var remaining = Math.Max(0, maxAttempts - count);

            return remaining > 0
                ? Allow(planName, isDemo: false, isFree: true, remainingAttempts: remaining)
                : Deny(planName, isFree: true, denyReason, 0);
        }

        private async Task<SubscriptionAccessResult> CheckExamAccessAsync(Guid? contentId, string planName)
        {
            // Gate quizzes are handled by GateQuizController and don't use Exam entities at all.
            // Here "Exams" means story/lesson comprehension exams.
            // Free users can access exams only for their allowed free content.
            if (contentId is null)
                return Allow(planName, isDemo: false, isFree: true);

            var exam = await db.Exams.FindAsync(contentId.Value);
            if (exam is null)
                return Allow(planName, isDemo: false, isFree: true); // unknown exam → allow

            // Check if the exam belongs to the first free story or first free booklet
            if (exam.StoryId.HasValue)
            {
                var freeStoryId = await db.Stories
                    .Where(s => s.IsPublished && s.Source == StorySource.PdfImport)
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefaultAsync();

                if (exam.StoryId == freeStoryId)
                    return Allow(planName, isDemo: false, isFree: true);
            }

            if (exam.LessonId.HasValue)
            {
                var freeBookletId = await db.Lessons
                    .Where(l => l.IsPublished)
                    .OrderBy(l => l.CreatedAt)
                    .Select(l => (Guid?)l.Id)
                    .FirstOrDefaultAsync();

                if (exam.LessonId == freeBookletId)
                    return Allow(planName, isDemo: false, isFree: true);
            }

            return Deny(planName, isFree: true,
                "الاختبارات متاحة بالكامل للمشتركين المدفوعين. يمكنك إجراء اختبار المحتوى التجريبي المجاني فقط.", null);
        }

        // ── Free-content ID sets ──────────────────────────────────────────────────

        private async Task<HashSet<Guid>> GetFreeLetterIdsAsync() =>
            (await db.LetterContents
                .Where(l => l.IsPublished)
                .OrderBy(l => l.SortOrder)
                .Take(SubscriptionConstants.FreeLettersLimit)
                .Select(l => l.Id)
                .ToListAsync())
            .ToHashSet();

        private async Task<HashSet<Guid>> GetFreeWordIdsAsync() =>
            (await db.WordContents
                .Where(w => w.IsPublished)
                .OrderBy(w => w.SortOrder)
                .Take(SubscriptionConstants.FreeWordsLimit)
                .Select(w => w.Id)
                .ToListAsync())
            .ToHashSet();

        private async Task<HashSet<Guid>> GetFreeSentenceIdsAsync() =>
            (await db.SentenceContents
                .Where(s => s.IsPublished)
                .OrderBy(s => s.SortOrder)
                .Take(SubscriptionConstants.FreeSentencesLimit)
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();

        // ── Attempt counters ──────────────────────────────────────────────────────

        private async Task<int> CountWritingAttemptsAsync(Guid studentId, Guid lessonPageId) =>
            await db.WritingAttempts
                .CountAsync(w => w.StudentId == studentId && w.LessonPageId == lessonPageId);

        private async Task<int> CountFluencyAttemptsAsync(Guid studentId, Guid pageId) =>
            await db.AudioRecordings
                .CountAsync(r => r.StudentId == studentId && r.PageId == pageId);

        // ── Result builders ───────────────────────────────────────────────────────

        private static bool IsFreeTierPlan(SubscriptionPlan? plan) =>
            plan is null
            or SubscriptionPlan.Free
            or SubscriptionPlan.TeacherFree
            or SubscriptionPlan.SchoolTrial;

        private static SubscriptionAccessResult Allow(
            string planName, bool isDemo, bool isFree, int? remainingAttempts = null) =>
            new(IsAllowed: true, Plan: planName, Reason: null,
                IsDemo: isDemo, IsFree: isFree, RemainingAttempts: remainingAttempts);

        private static SubscriptionAccessResult Deny(
            string planName, bool isFree, string reason, int? remainingAttempts) =>
            new(IsAllowed: false, Plan: planName, Reason: reason,
                IsDemo: false, IsFree: isFree, RemainingAttempts: remainingAttempts);
    }
}
