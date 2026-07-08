namespace Application.DTOs
{
    public record CreateActivationCodeRequest(
        string?   Code,
        string    Plan,
        int       DurationDays,
        int       MaxUses,
        DateTime? ExpiresAt,
        string?   Notes);

    public record ActivateSubscriptionRequest(string Code);

    /// <summary>
    /// Returned by ISubscriptionService.CheckAccessAsync.
    /// IsAllowed is always true in Phase 1 — enforcement comes in Phase 2.
    /// </summary>
    public record SubscriptionAccessResult(
        bool    IsAllowed,
        string  Plan,
        string? Reason,
        bool    IsDemo,
        bool    IsFree,
        int?    RemainingAttempts);

    /// <summary>
    /// Free-plan limits. Defined here for Phase 1; enforced in Phase 2.
    /// </summary>
    public static class SubscriptionConstants
    {
        public const int FreeLettersLimit   = 3;
        public const int FreeWordsLimit     = 3;
        public const int FreeSentencesLimit = 3;
        public const int FreeBookletsLimit  = 1;
        public const int FreeStoriesLimit   = 1;
        public const int FreeAiStoriesLimit = 1;
        public const int FreeMaxAttempts    = 2;

        public const int FreeParentMaxChildren             = 1;
        public const int FreeTeacherMaxStudents            = 5;
        public const int FreeTeacherMaxGroups              = 1;
        public const int FreeSchoolMaxClasses              = 1;
        public const int FreeSchoolMaxTeachers             = 1;

        public const int ParentPremiumMaxChildren          = 3;
        public const int TeacherPremiumMaxStudents         = 30;
        public const int TeacherPremiumMaxGroups           = 5;
        public const int SchoolPremiumMaxClasses           = 20;
        public const int SchoolPremiumMaxStudentsPerClass  = 30;
        public const int SchoolPremiumDefaultMaxTeachers   = 10;
    }
}
