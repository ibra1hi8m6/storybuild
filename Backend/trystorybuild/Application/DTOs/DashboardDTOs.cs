namespace Application.DTOs
{
    // ── Shared ─────────────────────────────────────────────────────────────────

    public record RecentActivityDto(
        string ActivityType,     // maps to frontend activityType
        string ChildName,
        string Title,
        double? Score,
        bool? IsAccepted,
        DateTime OccurredAt);   // maps to frontend occurredAt

    public record ExamHistoryDto(
        string StoryTitle,
        double Score,
        int CorrectAnswers,
        int TotalQuestions,
        DateTime CompletedAt);  // maps to frontend completedAt

    public record TopContentDto(
        string Id,
        string Title,
        string Type,
        int CompletionCount,
        double AvgScore);       // maps to frontend avgScore

    public record PerformanceBandDto(
        string Band,            // maps to frontend band (was Label)
        int Count,
        string Color);

    public record SkillBarDto(string Label, int Pct);

    // ── Student Dashboard ─────────────────────────────────────────────────────

    public record StudentDashboardDto(
        string ChildName,
        int Stars,
        int StoriesRead,              // frontend: storiesRead
        int LessonsCompleted,
        int ExamsCompleted,           // frontend: examsCompleted
        double AvgScore,              // frontend: avgScore
        int WritingAttempts,
        int WritingAccepted,
        double WritingAcceptanceRate,
        string PerformanceLevel,
        int CurrentStreak,
        int[] WeeklyActivity,         // [Mon..Sun] activity counts
        List<LessonSummaryDto> InProgressLessons,
        List<TopContentDto> TopStories,
        List<TopContentDto> TopLessons,
        List<ExamHistoryDto> ExamHistory,
        List<RecentActivityDto> RecentActivity,
        int Xp = 0,
        int DailyPagesGoal = 3,
        int DailyPagesDone = 0,
        List<string>? EarnedBadges = null);

    // ── Teacher-view student card ─────────────────────────────────────────────

    public record StudentSummaryDto(
        Guid Id,
        string ChildName,
        int Stars,
        int StoriesRead,
        int LessonsCompleted,
        double AvgScore,
        int WritingAccepted,
        int WritingAttempts,
        string PerformanceLevel,
        DateTime? LastActivity,
        int Level = 1,
        string? ClassroomName = null);

    // ── Parent Dashboard ──────────────────────────────────────────────────────

    public record ParentDashboardDto(
        string ChildName,
        int Stars,
        int StoriesRead,
        int LessonsCompleted,
        int ExamsCompleted,
        double AvgScore,
        int WritingAccepted,
        double WritingAcceptanceRate,
        string PerformanceLevel,
        int CurrentStreak,
        int[] WeeklyActivity,
        List<LessonSummaryDto> InProgressLessons,
        List<LessonAssignmentDto> RecentAssignments,
        List<SkillBarDto> SkillBars,
        List<TopContentDto> TopStories,
        List<ExamHistoryDto> ExamHistory,
        List<RecentActivityDto> RecentActivity,
        int LettersCompleted = 0,
        int LettersTotal = 0,
        int WordsCompleted = 0,
        int WordsTotal = 0,
        int SentencesCompleted = 0,
        int SentencesTotal = 0,
        int LessonsTotal = 0,
        int StoriesTotal = 0);

    // ── Teacher Dashboard ─────────────────────────────────────────────────────

    public record TeacherDashboardDto(
        int TotalStudents,
        int ActiveThisWeek,
        double AvgClassScore,
        List<TopContentDto> TopStories,
        List<TopContentDto> TopLessons,
        List<StudentSummaryDto> Students,
        List<PerformanceBandDto> PerformanceBands,
        double LettersAvgPct = 0,
        int LettersTotal = 0,
        double WordsAvgPct = 0,
        int WordsTotal = 0,
        double SentencesAvgPct = 0,
        int SentencesTotal = 0,
        double LessonsAvgPct = 0,
        int LessonsTotal = 0,
        double StoriesAvgPct = 0,
        int StoriesTotal = 0);

    // ── School Dashboard ──────────────────────────────────────────────────────

    public record SchoolDashboardDto(
        int TotalStudents,
        int TotalTeachers,
        int ActiveThisWeek,
        double AvgSchoolScore,
        int TotalStories,
        int TotalLessons,
        List<TopContentDto> TopContent,
        List<RecentActivityDto> RecentActivities,
        List<PerformanceBandDto> PerformanceBands,
        List<ClassroomStatsDto> Classrooms,
        List<LevelDistributionDto> LevelDistribution,
        double LettersAvgPct = 0,
        int LettersTotal = 0,
        double WordsAvgPct = 0,
        int WordsTotal = 0,
        double SentencesAvgPct = 0,
        int SentencesTotal = 0,
        double LessonsAvgPct = 0,
        int LessonsTotal = 0,
        double StoriesAvgPct = 0,
        int StoriesTotal = 0);

    public record ClassroomStatsDto(
        string Name,
        string Teacher,
        int Students,
        double AvgProgress);

    public record LevelDistributionDto(
        int Level,
        string Label,
        double Pct,
        string Color);

    // ── Assignment DTOs ────────────────────────────────────────────────────────

    public record AssignmentDto(
        Guid AssignmentId,
        Guid LessonId,
        string LessonTitle,
        string Letter,
        int Level,
        string TargetType,
        DateTime AssignedAt,
        bool IsSubmitted,
        double WritingScore,
        bool IsComplete);

    public record AssignmentSubmissionDto(
        Guid SubmissionId,
        Guid AssignmentId,
        Guid StudentId,
        string ChildName,
        int PagesCompleted,
        int TotalPages,
        double WritingScore,
        bool IsComplete,
        DateTime SubmittedAt);

    // ── Analytics DTOs ─────────────────────────────────────────────────────────

    public record WeakLetterDto(
        string Letter,
        int Attempts,
        int Correct,
        double Accuracy,
        string ActivityType,
        DateTime LastSeenAt);

    public record StudentAnalyticsDto(
        Guid StudentId,
        string ChildName,
        int Level,
        double OverallAccuracy,
        List<WeakLetterDto> WeakLetters);

    public record AnalyticsSummaryDto(
        int TotalStudents,
        double ClassAvgAccuracy,
        List<StudentAnalyticsDto> Students,
        List<WeakLetterDto> MostCommonWeakLetters);

    // ── Level Progress (for /levels page) ────────────────────────────────────

    public record LevelProgressDto(
        int Level,
        string Title,
        string Subtitle,
        string Icon,
        string Tag,
        bool Locked,
        int Stars,
        int TotalStars,
        int LessonsCompleted,
        int TotalLessons,
        double AvgScore,
        string? UnlockCondition);
}
