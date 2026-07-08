using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DashboardService(
        AppDbContext db,
        ILogger<DashboardService> logger) : IDashboardService
    {
        // ── Student ───────────────────────────────────────────────────────────
        public async Task<StudentDashboardDto?> GetStudentDashboardAsync(Guid studentId)
        {
            var student = await db.Students.FindAsync(studentId);
            if (student is null) return null;
            var name = student.Name;

            var progress = await db.StudentProgress.Where(p => p.StudentId == studentId).ToListAsync();

            if (progress.Count == 0 && !await HasAnyActivityAsync(studentId))
                return null;

            var writing      = await db.WritingAttempts.Where(w => w.StudentId == studentId).ToListAsync();
            var completions  = await db.StudentContentCompletions.Where(c => c.StudentId == studentId).ToListAsync();

            int storiesRead      = completions.Count(c => c.ContentType == ContentCompletionType.Story);
            int lessonsCompleted = completions.Count(c => c.ContentType == ContentCompletionType.Lesson);
            int examsCompleted   = progress.Count(p => p.ExamCompleted);
            double avgScore      = await ComputeWritingAvgAsync(studentId);
            int writingAccepted  = writing.Count(w => w.IsAccepted);
            int stars            = CalculateStars(progress, writing);
            int xp               = stars * 10;

            var today          = DateTime.UtcNow.Date;
            int dailyPagesDone = await db.LessonPageCompletions
                .CountAsync(c => c.StudentId == studentId && c.CompletedAt >= today);

            var badges = ComputeBadges(storiesRead, lessonsCompleted, writingAccepted, stars, xp);

            return new StudentDashboardDto(
                name, stars,
                storiesRead, lessonsCompleted, examsCompleted,
                Math.Round(avgScore, 1),
                writing.Count, writingAccepted,
                writing.Count > 0 ? Math.Round((double)writingAccepted / writing.Count * 100, 1) : 0,
                GetPerformanceLevel(avgScore),
                await CalculateStreakAsync(studentId),
                await BuildWeeklyActivityAsync(studentId),
                await GetInProgressLessonsAsync(studentId),
                await GetStudentTopContentAsync(studentId, storyOnly: true),
                await GetStudentTopContentAsync(studentId, storyOnly: false),
                await BuildExamHistoryAsync(studentId),
                await BuildRecentActivityAsync(studentId, name),
                Xp: xp,
                DailyPagesGoal: 3,
                DailyPagesDone: dailyPagesDone,
                EarnedBadges: badges);
        }

        // ── Parent ────────────────────────────────────────────────────────────
        public async Task<ParentDashboardDto?> GetParentDashboardAsync(Guid studentId)
        {
            var student = await db.Students.FindAsync(studentId);
            if (student is null) return null;
            var name = student.Name;

            var progress = await db.StudentProgress.Where(p => p.StudentId == studentId).ToListAsync();
            var writing  = await db.WritingAttempts.Where(w => w.StudentId == studentId).ToListAsync();

            // Content completions from new tracking system
            var completions = await db.StudentContentCompletions
                .Where(c => c.StudentId == studentId)
                .GroupBy(c => c.ContentType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            int lettersCompleted   = completions.FirstOrDefault(g => g.Type == ContentCompletionType.Letter)?.Count   ?? 0;
            int wordsCompleted     = completions.FirstOrDefault(g => g.Type == ContentCompletionType.Word)?.Count     ?? 0;
            int sentencesCompleted = completions.FirstOrDefault(g => g.Type == ContentCompletionType.Sentence)?.Count ?? 0;
            int lessonsCompleted   = completions.FirstOrDefault(g => g.Type == ContentCompletionType.Lesson)?.Count   ?? 0;
            int storiesCompleted   = completions.FirstOrDefault(g => g.Type == ContentCompletionType.Story)?.Count    ?? 0;

            int lettersTotal   = await db.LetterContents.CountAsync(l => l.IsPublished);
            int wordsTotal     = await db.WordContents.CountAsync(w => w.IsPublished);
            int sentencesTotal = await db.SentenceContents.CountAsync(s => s.IsPublished);
            int lessonsTotal   = await db.Lessons.CountAsync(l => l.IsPublished);
            int storiesTotal   = await db.Stories.CountAsync(s => s.IsPublished && s.Source == StorySource.PdfImport);

            double avgScore = await ComputeWritingAvgAsync(studentId);
            int writingAcc  = writing.Count(w => w.IsAccepted);

            var fluencyAccList = await db.FluencyReports
                .Join(db.AudioRecordings, f => f.RecordingId, r => r.Id,
                      (f, r) => new { f.AccuracyScore, r.StudentId })
                .Where(x => x.StudentId == studentId)
                .Select(x => x.AccuracyScore)
                .ToListAsync();
            double avgReadingAccuracy = fluencyAccList.Any() ? fluencyAccList.Average() : 0;

            return new ParentDashboardDto(
                name,
                CalculateStars(progress, writing),
                storiesCompleted, lessonsCompleted, 0,
                avgScore,
                writingAcc,
                writing.Count > 0 ? Math.Round((double)writingAcc / writing.Count * 100, 1) : 0,
                GetPerformanceLevel(avgScore),
                await CalculateStreakAsync(studentId),
                await BuildWeeklyActivityAsync(studentId),
                await GetInProgressLessonsAsync(studentId),
                new List<LessonAssignmentDto>(),
                BuildSkillBars(lettersCompleted, lettersTotal,
                               wordsCompleted, wordsTotal,
                               sentencesCompleted, sentencesTotal,
                               avgScore, avgReadingAccuracy),
                await GetStudentTopContentAsync(studentId, storyOnly: true),
                await BuildExamHistoryAsync(studentId),
                await BuildRecentActivityAsync(studentId, name),
                lettersCompleted, lettersTotal,
                wordsCompleted, wordsTotal,
                sentencesCompleted, sentencesTotal,
                lessonsTotal, storiesTotal);
        }

        // ── Teacher ───────────────────────────────────────────────────────────
        public async Task<TeacherDashboardDto> GetTeacherDashboardAsync(Guid teacherId)
        {
            // Source 1: direct students (TeacherId == teacherId)
            var directStudents = await db.Students
                .Where(s => s.TeacherId == teacherId)
                .Select(s => new { s.Id, s.Name, s.Level })
                .ToListAsync();

            // Source 2: students in this teacher's groups (private teacher groups)
            var groupStudentIds = await db.StudentGroupMembers
                .Where(m => m.Group.TeacherId == teacherId)
                .Select(m => m.StudentId)
                .Distinct()
                .ToListAsync();

            var groupStudents = groupStudentIds.Any()
                ? await db.Students
                    .Where(s => groupStudentIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name, s.Level })
                    .ToListAsync()
                : new();

            // Source 3: students in classrooms assigned to this teacher (school teacher), with classroom name and level
            var classroomData = await db.Classrooms
                .Where(c => c.TeacherId == teacherId)
                .Select(c => new { c.Id, c.Name, c.Level, StudentIds = c.Students.Select(cs => cs.StudentId).ToList() })
                .ToListAsync();

            var classroomStudentMap = new Dictionary<Guid, (Guid ClassroomId, string Name)>(); // studentId → (classroomId, classroomName)
            foreach (var cls in classroomData)
                foreach (var sid in cls.StudentIds)
                    classroomStudentMap.TryAdd(sid, (cls.Id, cls.Name));

            var classroomStudentIds = classroomStudentMap.Keys.ToList();
            var classroomStudents = classroomStudentIds.Any()
                ? await db.Students
                    .Where(s => classroomStudentIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name, s.Level })
                    .ToListAsync()
                : new();

            var childEntries = directStudents
                .Concat(groupStudents)
                .Concat(classroomStudents)
                .DistinctBy(e => e.Id)
                .ToList();
            var childIds = childEntries.Select(e => e.Id).ToList();
            int n        = childIds.Count;

            // Active this week from page completions
            var cutoff   = DateTime.UtcNow.AddDays(-7);
            int activeWeek = await db.LessonPageCompletions
                .Where(c => c.CompletedAt >= cutoff && c.StudentId.HasValue && childIds.Contains(c.StudentId.Value))
                .Select(c => c.StudentId).Distinct().CountAsync();

            // Content completions aggregate for class-wide progress cards
            var completionsPerType = await db.StudentContentCompletions
                .Where(c => childIds.Contains(c.StudentId))
                .GroupBy(c => c.ContentType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            int lettersCompleted   = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Letter)?.Count   ?? 0;
            int wordsCompleted     = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Word)?.Count     ?? 0;
            int sentencesCompleted = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Sentence)?.Count ?? 0;
            int lessonsCompleted   = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Lesson)?.Count   ?? 0;
            int storiesCompleted   = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Story)?.Count    ?? 0;

            int lettersTotal   = await db.LetterContents.CountAsync(l => l.IsPublished);
            int wordsTotal     = await db.WordContents.CountAsync(w => w.IsPublished);
            int sentencesTotal = await db.SentenceContents.CountAsync(s => s.IsPublished);
            int lessonsTotal   = await db.Lessons.CountAsync(l => l.IsPublished);
            int storiesTotal   = await db.Stories.CountAsync(s => s.IsPublished && s.Source == StorySource.PdfImport);

            double lettersAvgPct   = n > 0 && lettersTotal   > 0 ? Math.Min(100, Math.Round((double)lettersCompleted   / (n * lettersTotal)   * 100, 1)) : 0;
            double wordsAvgPct     = n > 0 && wordsTotal     > 0 ? Math.Min(100, Math.Round((double)wordsCompleted     / (n * wordsTotal)     * 100, 1)) : 0;
            double sentencesAvgPct = n > 0 && sentencesTotal > 0 ? Math.Min(100, Math.Round((double)sentencesCompleted / (n * sentencesTotal) * 100, 1)) : 0;
            double lessonsAvgPct   = n > 0 && lessonsTotal   > 0 ? Math.Min(100, Math.Round((double)lessonsCompleted   / (n * lessonsTotal)   * 100, 1)) : 0;
            double storiesAvgPct   = n > 0 && storiesTotal   > 0 ? Math.Min(100, Math.Round((double)storiesCompleted   / (n * storiesTotal)   * 100, 1)) : 0;

            double avgScore = await ComputeWritingAvgForManyAsync(childIds);

            logger.LogInformation("[Dashboard] Teacher — {Count} students, avg {Avg}%", n, avgScore);

            var students = new List<StudentSummaryDto>();
            foreach (var entry in childEntries)
            {
                classroomStudentMap.TryGetValue(entry.Id, out var clsInfo);
                students.Add(await BuildStudentSummaryAsync(
                    entry.Id, entry.Name, entry.Level,
                    clsInfo.Name,
                    clsInfo.ClassroomId == Guid.Empty ? null : clsInfo.ClassroomId));
            }

            // Build classroom groups for school teachers — includes empty classrooms
            List<ClassroomGroupDto>? classroomGroups = null;
            if (classroomData.Count > 0)
            {
                var studentsByClassroomId = students
                    .Where(s => s.ClassroomId.HasValue)
                    .GroupBy(s => s.ClassroomId!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Stars).ToList());

                classroomGroups = classroomData.Select(cls =>
                {
                    var clsStudents = studentsByClassroomId.TryGetValue(cls.Id, out var s) ? s : new List<StudentSummaryDto>();
                    return new ClassroomGroupDto(cls.Id, cls.Name, cls.Level, clsStudents.Count, clsStudents);
                }).ToList();
            }

            return new TeacherDashboardDto(
                n, activeWeek, avgScore,
                await BuildTopStoriesAsync(),
                await BuildTopLessonsAsync(),
                students.OrderByDescending(s => s.Stars).ToList(),
                BuildPerformanceBands([]),
                lettersAvgPct, lettersTotal,
                wordsAvgPct, wordsTotal,
                sentencesAvgPct, sentencesTotal,
                lessonsAvgPct, lessonsTotal,
                storiesAvgPct, storiesTotal,
                classroomGroups);
        }

        // ── School ────────────────────────────────────────────────────────────
        public async Task<SchoolDashboardDto> GetSchoolDashboardAsync(Guid schoolManagerId)
        {
            // Teachers who belong to this school
            var schoolTeacherIds = await db.Teachers
                .Where(t => t.SchoolManagerId == schoolManagerId)
                .Select(t => t.Id)
                .ToListAsync();

            int totalTeachers = await db.Users
                .CountAsync(u => schoolTeacherIds.Contains(u.Id) && u.IsActive);

            // Classroom IDs for this school
            var schoolClassroomIds = await db.Classrooms
                .Where(c => c.SchoolManagerId == schoolManagerId)
                .Select(c => c.Id)
                .ToListAsync();

            // Students via classroom enrollment (what the UI shows in the classrooms table)
            var classroomStudentIds = await db.ClassroomStudents
                .Where(cs => schoolClassroomIds.Contains(cs.ClassroomId))
                .Select(cs => cs.StudentId)
                .Distinct()
                .ToListAsync();

            // Students directly created by school teachers (may not be in any classroom yet)
            var teacherStudentIds = await db.Students
                .Where(s => s.TeacherId.HasValue && schoolTeacherIds.Contains(s.TeacherId!.Value))
                .Select(s => s.Id)
                .ToListAsync();

            // Union both sources — classroom-enrolled + directly under teacher
            var schoolStudentIds = classroomStudentIds.Union(teacherStudentIds).Distinct().ToList();
            int totalStudents = schoolStudentIds.Count;

            var cutoff     = DateTime.UtcNow.AddDays(-7);
            int activeWeek = await db.LessonPageCompletions
                .Where(c => c.CompletedAt >= cutoff && c.StudentId.HasValue && schoolStudentIds.Contains(c.StudentId.Value))
                .Select(c => c.StudentId).Distinct().CountAsync();

            // Content completions aggregate for school-wide progress cards
            var completionsPerType = await db.StudentContentCompletions
                .Where(c => schoolStudentIds.Contains(c.StudentId))
                .GroupBy(c => c.ContentType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            int lettersCompleted   = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Letter)?.Count   ?? 0;
            int wordsCompleted     = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Word)?.Count     ?? 0;
            int sentencesCompleted = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Sentence)?.Count ?? 0;
            int lessonsCompleted   = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Lesson)?.Count   ?? 0;
            int storiesCompleted   = completionsPerType.FirstOrDefault(g => g.Type == ContentCompletionType.Story)?.Count    ?? 0;

            int lettersTotal   = await db.LetterContents.CountAsync(l => l.IsPublished);
            int wordsTotal     = await db.WordContents.CountAsync(w => w.IsPublished);
            int sentencesTotal = await db.SentenceContents.CountAsync(s => s.IsPublished);
            int lessonsTotal   = await db.Lessons.CountAsync(l => l.IsPublished);
            int storiesTotal   = await db.Stories.CountAsync(s => s.IsPublished && s.Source == StorySource.PdfImport);
            int n              = totalStudents;

            double lettersAvgPct   = n > 0 && lettersTotal   > 0 ? Math.Min(100, Math.Round((double)lettersCompleted   / (n * lettersTotal)   * 100, 1)) : 0;
            double wordsAvgPct     = n > 0 && wordsTotal     > 0 ? Math.Min(100, Math.Round((double)wordsCompleted     / (n * wordsTotal)     * 100, 1)) : 0;
            double sentencesAvgPct = n > 0 && sentencesTotal > 0 ? Math.Min(100, Math.Round((double)sentencesCompleted / (n * sentencesTotal) * 100, 1)) : 0;
            double lessonsAvgPct   = n > 0 && lessonsTotal   > 0 ? Math.Min(100, Math.Round((double)lessonsCompleted   / (n * lessonsTotal)   * 100, 1)) : 0;
            double storiesAvgPct   = n > 0 && storiesTotal   > 0 ? Math.Min(100, Math.Round((double)storiesCompleted   / (n * storiesTotal)   * 100, 1)) : 0;

            double avgScore = await ComputeWritingAvgForManyAsync(schoolStudentIds);

            var topContent = (await BuildTopStoriesAsync())
                .Concat(await BuildTopLessonsAsync())
                .OrderByDescending(t => t.CompletionCount).Take(5).ToList();

            return new SchoolDashboardDto(
                totalStudents, totalTeachers, activeWeek,
                avgScore,
                await db.Stories.CountAsync(s => s.IsPublished && s.Source == StorySource.PdfImport),
                await db.Lessons.CountAsync(l => l.IsPublished),
                topContent,
                new List<RecentActivityDto>(),
                BuildPerformanceBands([]),
                await GetClassroomsAsync(schoolManagerId),
                await GetLevelDistributionAsync(schoolStudentIds),
                lettersAvgPct, lettersTotal,
                wordsAvgPct, wordsTotal,
                sentencesAvgPct, sentencesTotal,
                lessonsAvgPct, lessonsTotal,
                storiesAvgPct, storiesTotal);
        }

        // ── Level Progress ────────────────────────────────────────────────────
        public async Task<List<LevelProgressDto>> GetLevelProgressAsync(Guid studentId)
        {
            var student      = await db.Students.FindAsync(studentId);
            int placementLvl = student?.Level ?? 1;

            var doneProgress = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.LessonId.HasValue && p.ExamCompleted)
                .Include(p => p.Lesson)
                .ToListAsync();

            var levelCounts = await db.Lessons
                .Where(l => !l.IsGenerated)
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Total = g.Count() })
                .ToListAsync();

            var defs = new[]
            {
                new { Level=1, Title="الحروف",   Subtitle="أتقن كل 28 حرفاً عربياً", Icon="📖", Tag="حروف" },
                new { Level=2, Title="الكلمات والجمل", Subtitle="تعلم أكثر من 200 كلمة",   Icon="📝", Tag="كلمات" },
                new { Level=3, Title=" كتيبات والقصص",      Subtitle="اقرأ واكتب جملاً وقصصاً", Icon="📚", Tag="جمل"  },
            };

            var result = new List<LevelProgressDto>();

            foreach (var d in defs)
            {
                var lp        = doneProgress.Where(p => p.Lesson?.Level == d.Level).ToList();
                int completed = lp.Count;
                int total     = levelCounts.FirstOrDefault(x => x.Level == d.Level)?.Total ?? 0;
                double avg    = lp.Any() ? lp.Average(p => p.ScorePercentage) : 0;
                int stars     = lp.Sum(p => p.ScorePercentage >= 90 ? 3 : p.ScorePercentage >= 70 ? 2 : p.ScorePercentage >= 50 ? 1 : 0);
                bool locked   = d.Level > placementLvl && completed == 0;

                result.Add(new LevelProgressDto(
                    d.Level, d.Title, d.Subtitle, d.Icon,
                    locked ? "مغلق" : d.Tag,
                    locked, stars, total * 3,
                    completed, total,
                    Math.Round(avg, 1),
                    locked ? $"أكمل دروس المستوى {d.Level - 1} للوصول إليه" : null));
            }
            return result;
        }

        // ── Known names ───────────────────────────────────────────────────────
        public async Task<List<string>> GetKnownChildNamesAsync()
        {
            var a = await db.StudentProgress.Select(p => p.ChildName).Distinct().ToListAsync();
            var b = await db.WritingAttempts.Select(w => w.ChildName).Distinct().ToListAsync();
            return a.Union(b).Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n).ToList();
        }

        // ── Writing average (category-weighted) ──────────────────────────────

        // Average of category averages so no single content type dominates.
        // Only categories that have at least one attempt contribute.
        // Categories: letters (LetterSound+LetterRecognition), words, sentences, lessons (WritingAttempt).
        private async Task<double> ComputeWritingAvgAsync(Guid studentId)
        {
            var learningWriting = await db.LearningAttempts
                .Where(a => a.StudentId == studentId && a.AttemptType == LearningAttemptType.Writing)
                .Select(a => new { a.ContentType, a.Score })
                .ToListAsync();

            var lessonScores = await db.WritingAttempts
                .Where(w => w.StudentId == studentId)
                .Select(w => w.SimilarityScore)
                .ToListAsync();

            var categoryAvgs = new List<double>();

            var letterScores = learningWriting
                .Where(a => a.ContentType == LearningContentType.LetterSound
                         || a.ContentType == LearningContentType.LetterRecognition)
                .Select(a => Math.Clamp(a.Score, 0, 100)).ToList();
            if (letterScores.Count > 0) categoryAvgs.Add(letterScores.Average());

            var wordScores = learningWriting
                .Where(a => a.ContentType == LearningContentType.WordPractice)
                .Select(a => Math.Clamp(a.Score, 0, 100)).ToList();
            if (wordScores.Count > 0) categoryAvgs.Add(wordScores.Average());

            var sentenceScores = learningWriting
                .Where(a => a.ContentType == LearningContentType.SentencePractice)
                .Select(a => Math.Clamp(a.Score, 0, 100)).ToList();
            if (sentenceScores.Count > 0) categoryAvgs.Add(sentenceScores.Average());

            if (lessonScores.Count > 0) categoryAvgs.Add(lessonScores.Select(s => Math.Clamp(s, 0, 100)).Average());

            return categoryAvgs.Count > 0 ? Math.Round(categoryAvgs.Average(), 1) : 0;
        }

        private async Task<double> ComputeWritingAvgForManyAsync(List<Guid> studentIds)
        {
            if (studentIds.Count == 0) return 0;

            var learningWriting = await db.LearningAttempts
                .Where(a => a.StudentId.HasValue && studentIds.Contains(a.StudentId.Value)
                         && a.AttemptType == LearningAttemptType.Writing)
                .Select(a => new { a.ContentType, a.Score })
                .ToListAsync();

            var lessonScores = await db.WritingAttempts
                .Where(w => w.StudentId.HasValue && studentIds.Contains(w.StudentId.Value))
                .Select(w => w.SimilarityScore)
                .ToListAsync();

            var categoryAvgs = new List<double>();

            var letterScores = learningWriting
                .Where(a => a.ContentType == LearningContentType.LetterSound
                         || a.ContentType == LearningContentType.LetterRecognition)
                .Select(a => Math.Clamp(a.Score, 0, 100)).ToList();
            if (letterScores.Count > 0) categoryAvgs.Add(letterScores.Average());

            var wordScores = learningWriting
                .Where(a => a.ContentType == LearningContentType.WordPractice)
                .Select(a => Math.Clamp(a.Score, 0, 100)).ToList();
            if (wordScores.Count > 0) categoryAvgs.Add(wordScores.Average());

            var sentenceScores = learningWriting
                .Where(a => a.ContentType == LearningContentType.SentencePractice)
                .Select(a => Math.Clamp(a.Score, 0, 100)).ToList();
            if (sentenceScores.Count > 0) categoryAvgs.Add(sentenceScores.Average());

            if (lessonScores.Count > 0) categoryAvgs.Add(lessonScores.Select(s => Math.Clamp(s, 0, 100)).Average());

            return categoryAvgs.Count > 0 ? Math.Round(categoryAvgs.Average(), 1) : 0;
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private async Task<bool> HasAnyActivityAsync(Guid studentId) =>
            await db.WritingAttempts.AnyAsync(w => w.StudentId == studentId)
            || await db.LearningAttempts.AnyAsync(a => a.StudentId == studentId)
            || await db.StudentProgress.AnyAsync(p => p.StudentId == studentId)
            || await db.StudentContentCompletions.AnyAsync(c => c.StudentId == studentId);

        private async Task<int[]> BuildWeeklyActivityAsync(Guid studentId)
        {
            var since = DateTime.UtcNow.AddDays(-6);
            var eDates = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.ExamCompleted && p.LastUpdatedAt >= since)
                .Select(p => p.LastUpdatedAt).ToListAsync();
            var wDates = await db.WritingAttempts
                .Where(w => w.StudentId == studentId && w.AttemptedAt >= since)
                .Select(w => w.AttemptedAt).ToListAsync();
            var cDates = await db.StudentContentCompletions
                .Where(c => c.StudentId == studentId && c.CompletedAt >= since)
                .Select(c => c.CompletedAt).ToListAsync();

            var act = new int[7];
            foreach (var dt in eDates.Concat(wDates).Concat(cDates))
                act[((int)dt.DayOfWeek + 6) % 7]++;
            return act;
        }

        private async Task<int> CalculateStreakAsync(Guid studentId)
        {
            var eDates = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.ExamCompleted)
                .Select(p => p.LastUpdatedAt.Date).ToListAsync();
            var wDates = await db.WritingAttempts
                .Where(w => w.StudentId == studentId)
                .Select(w => w.AttemptedAt.Date).ToListAsync();
            var cDates = await db.StudentContentCompletions
                .Where(c => c.StudentId == studentId)
                .Select(c => c.CompletedAt.Date).ToListAsync();

            var days = eDates.Concat(wDates).Concat(cDates).Distinct()
                .OrderByDescending(d => d).ToList();
            if (days.Count == 0) return 0;

            var today = DateTime.UtcNow.Date;
            if (days[0] < today.AddDays(-1)) return 0;

            int streak = 0; var exp = today;
            foreach (var day in days)
            {
                if (day >= exp.AddDays(-1) && day <= exp)
                { streak++; exp = day.AddDays(-1); }
                else break;
            }
            return streak;
        }

        private async Task<List<LessonSummaryDto>> GetInProgressLessonsAsync(Guid studentId)
        {
            var startedIds = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.LessonId.HasValue && !p.ExamCompleted)
                .Select(p => p.LessonId!.Value).ToListAsync();

            if (startedIds.Count > 0)
            {
                var lessons = await db.Lessons.Include(l => l.Pages)
                    .Where(l => startedIds.Contains(l.Id)).Take(5).ToListAsync();
                return lessons.Select(ToSummaryDto).ToList();
            }

            var doneIds = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.LessonId.HasValue && p.ExamCompleted)
                .Select(p => p.LessonId!.Value).ToListAsync();

            var recs = await db.Lessons.Include(l => l.Pages)
                .Where(l => l.Level == 1 && !l.IsGenerated && !doneIds.Contains(l.Id))
                .Take(5).ToListAsync();
            return recs.Select(ToSummaryDto).ToList();
        }

        private async Task<List<TopContentDto>> GetStudentTopContentAsync(Guid studentId, bool storyOnly)
        {
            var items = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.ExamCompleted
                    && (storyOnly ? p.StoryId.HasValue : p.LessonId.HasValue))
                .Include(p => p.Story).Include(p => p.Lesson)
                .OrderByDescending(p => p.ScorePercentage)
                .Take(5).ToListAsync();

            return items.Select(p => new TopContentDto(
                (storyOnly ? p.StoryId : p.LessonId)!.Value.ToString(),
                (storyOnly ? p.Story?.Title : p.Lesson?.Title) ?? "—",
                storyOnly ? "story" : "lesson",
                1, Math.Round(p.ScorePercentage, 1))).ToList();
        }

        private async Task<List<ExamHistoryDto>> BuildExamHistoryAsync(Guid studentId)
        {
            var items = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.ExamCompleted)
                .Include(p => p.Story).Include(p => p.Lesson)
                .OrderByDescending(p => p.LastUpdatedAt)
                .Take(10).ToListAsync();

            return items.Select(p => new ExamHistoryDto(
                p.Story?.Title ?? p.Lesson?.Title ?? "امتحان",
                p.ScorePercentage, p.CorrectAnswers, p.TotalQuestions,
                p.LastUpdatedAt)).ToList();
        }

        private async Task<List<RecentActivityDto>> BuildRecentActivityAsync(Guid studentId, string name)
        {
            var list = new List<RecentActivityDto>();

            var prog = await db.StudentProgress
                .Where(p => p.StudentId == studentId && p.ExamCompleted)
                .Include(p => p.Story).Include(p => p.Lesson).ToListAsync();

            list.AddRange(prog.Select(p => new RecentActivityDto(
                "exam", name,
                p.Story?.Title ?? p.Lesson?.Title ?? "امتحان",
                p.ScorePercentage, null, p.LastUpdatedAt)));

            var writings = await db.WritingAttempts.Where(w => w.StudentId == studentId).ToListAsync();
            list.AddRange(writings.Select(w => new RecentActivityDto(
                "writing", name, w.ExpectedSentence, w.SimilarityScore, w.IsAccepted, w.AttemptedAt)));

            var newCompletions = await db.StudentContentCompletions.Where(c => c.StudentId == studentId).ToListAsync();
            list.AddRange(newCompletions.Select(c => new RecentActivityDto(
                c.ContentType.ToString().ToLowerInvariant(), name,
                c.ContentType switch {
                    ContentCompletionType.Letter   => "حرف مكتمل",
                    ContentCompletionType.Word     => "كلمة مكتملة",
                    ContentCompletionType.Sentence => "جملة مكتملة",
                    ContentCompletionType.Lesson   => "درس مكتمل",
                    ContentCompletionType.Story    => "قصة مكتملة",
                    _                              => "نشاط مكتمل"
                }, null, null, c.CompletedAt)));

            return list.OrderByDescending(a => a.OccurredAt).Take(15).ToList();
        }

        private async Task<StudentSummaryDto> BuildStudentSummaryAsync(Guid id, string name, int level = 1, string? classroomName = null, Guid? classroomId = null)
        {
            var progress = await db.StudentProgress.Where(p => p.StudentId == id).ToListAsync();
            var writing  = await db.WritingAttempts.Where(w => w.StudentId == id).ToListAsync();
            double avg   = await ComputeWritingAvgAsync(id);
            DateTime? last = writing.Any()
                ? (DateTime?)writing.Max(w => w.AttemptedAt)
                : progress.Any() ? (DateTime?)progress.Max(p => p.LastUpdatedAt) : null;

            var completions = await db.StudentContentCompletions.Where(c => c.StudentId == id).ToListAsync();
            int storiesDone  = completions.Count(c => c.ContentType == ContentCompletionType.Story);
            int lessonsDone  = completions.Count(c => c.ContentType == ContentCompletionType.Lesson);

            return new StudentSummaryDto(
                id, name, CalculateStars(progress, writing),
                storiesDone, lessonsDone,
                avg,
                writing.Count(w => w.IsAccepted), writing.Count,
                GetPerformanceLevel(avg), last, level, classroomName, classroomId);
        }

        private async Task<List<TopContentDto>> BuildTopStoriesAsync() =>
            await db.StudentProgress
                .Where(p => p.ExamCompleted && p.StoryId.HasValue)
                .GroupBy(p => p.StoryId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count(), Avg = g.Average(p => p.ScorePercentage) })
                .OrderByDescending(x => x.Count).Take(5)
                .Join(db.Stories, x => x.Id, s => s.Id,
                    (x, s) => new TopContentDto(s.Id.ToString(), s.Title, "story", x.Count, Math.Round(x.Avg, 1)))
                .ToListAsync();

        private async Task<List<TopContentDto>> BuildTopLessonsAsync()
        {
            var atts = await db.WritingAttempts
                .Join(db.LessonPages, w => w.LessonPageId, p => p.Id,
                    (w, p) => new { w.IsAccepted, p.LessonId })
                .GroupBy(x => x.LessonId)
                .Select(g => new { LessonId = g.Key, Count = g.Count(), Avg = g.Count(x => x.IsAccepted) * 100.0 / g.Count() })
                .OrderByDescending(x => x.Count).Take(5).ToListAsync();

            var ids     = atts.Select(x => x.LessonId).ToList();
            var lessons = await db.Lessons.Where(l => ids.Contains(l.Id)).ToListAsync();

            return atts.Join(lessons, x => x.LessonId, l => l.Id,
                (x, l) => new TopContentDto(l.Id.ToString(), l.Title, "lesson", x.Count, Math.Round(x.Avg, 1)))
                .ToList();
        }

        private async Task<List<ClassroomStatsDto>> GetClassroomsAsync(Guid schoolManagerId)
        {
            var classrooms = await db.Classrooms
                .Where(c => c.SchoolManagerId == schoolManagerId)
                .Include(c => c.Students).ThenInclude(cs => cs.Student)
                .OrderBy(c => c.Level).ThenBy(c => c.Name)
                .Take(10)
                .ToListAsync();

            if (classrooms.Count == 0) return new();

            var teacherIds = classrooms.Select(c => c.TeacherId).Distinct().ToList();
            var teacherMap = await db.Users.Where(u => teacherIds.Contains(u.Id))
                                           .ToDictionaryAsync(u => u.Id, u => u.Name);

            var allStudentIds = classrooms.SelectMany(c => c.Students.Select(cs => cs.StudentId)).Distinct().ToList();

            var result = new List<ClassroomStatsDto>();
            foreach (var c in classrooms)
            {
                var ids    = c.Students.Select(cs => cs.StudentId).ToList();
                double avg = await ComputeWritingAvgForManyAsync(ids);
                var teacherName = teacherMap.GetValueOrDefault(c.TeacherId, "");
                result.Add(new ClassroomStatsDto(c.Name, teacherName, ids.Count, avg));
            }
            return result;
        }

        private async Task<List<LevelDistributionDto>> GetLevelDistributionAsync(List<Guid> schoolStudentIds)
        {
            var counts = await db.Students
                .Where(s => schoolStudentIds.Contains(s.Id))
                .GroupBy(s => s.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() }).ToListAsync();

            int total  = counts.Sum(x => x.Count);
            var colors = new[] { "#F4788A", "#C4B5FD", "#86EFAC" };
            var labels = new[] { "المستوى 1", "المستوى 2", "المستوى 3" };
            var result = new List<LevelDistributionDto>();

            for (int i = 1; i <= 3; i++)
            {
                int cnt  = counts.FirstOrDefault(x => x.Level == i)?.Count ?? 0;
                double p = total > 0 ? Math.Round((double)cnt / total * 100, 1) : 0;
                result.Add(new LevelDistributionDto(i, labels[i-1], p, colors[i-1]));
            }
            return result;
        }

        private static List<PerformanceBandDto> BuildPerformanceBands(
            List<Domain.Entities.StudentProgress> p)
        {
            if (!p.Any()) return new();
            return new()
            {
                new("ممتاز (80٪+)",       p.Count(x => x.ScorePercentage >= 80),             "#48bb78"),
                new("جيد (50-79٪)",        p.Count(x => x.ScorePercentage >= 50 && x.ScorePercentage < 80), "#ed8936"),
                new("يحتاج تحسين (<50٪)", p.Count(x => x.ScorePercentage < 50),              "#fc8181"),
            };
        }

        private static List<SkillBarDto> BuildSkillBars(
            int lettersCompleted, int lettersTotal,
            int wordsCompleted,   int wordsTotal,
            int sentencesCompleted, int sentencesTotal,
            double writingAvgScore,
            double avgReadingAccuracy)
        {
            double letterPct  = lettersTotal > 0
                ? Math.Round((double)lettersCompleted / lettersTotal * 100) : 0;
            double vocabPct   = (wordsTotal + sentencesTotal) > 0
                ? Math.Round((double)(wordsCompleted + sentencesCompleted) / (wordsTotal + sentencesTotal) * 100) : 0;
            double writePct   = Math.Round(writingAvgScore);
            double readingPct = Math.Round(avgReadingAccuracy);
            return new()
            {
                new("التعرف على الحروف", (int)Math.Min(100, letterPct)),
                new("طلاقة القراءة",     (int)Math.Min(100, readingPct)),
                new("تدريب الكتابة",     (int)Math.Min(100, writePct)),
                new("المفردات",           (int)Math.Min(100, vocabPct)),
            };
        }

        private static LessonSummaryDto ToSummaryDto(Domain.Entities.Lesson l) => new(
            l.Id, l.Level, l.Letter, l.LetterName, l.Title,
            l.Pages.FirstOrDefault(p => p.IsCoverPage)?.ImagePath
                ?? l.Pages.FirstOrDefault()?.ImagePath ?? "",
            l.Pages.Count);

        private static int CalculateStars(
            List<Domain.Entities.StudentProgress> p,
            List<Domain.Entities.WritingAttempt>  w)
        {
            int stars = p.Where(x => x.ExamCompleted)
                .Sum(x => x.ScorePercentage >= 90 ? 3 : x.ScorePercentage >= 70 ? 2 : x.ScorePercentage >= 50 ? 1 : 0);
            return stars + w.Count(x => x.IsAccepted);
        }

        private static string GetPerformanceLevel(double avg) =>
            avg >= 80 ? "ممتاز" : avg >= 50 ? "جيد" : "يحتاج تحسين";

        private static List<string> ComputeBadges(
            int storiesRead, int lessonsCompleted, int writingAccepted, int stars, int xp)
        {
            var earned = new List<string>();
            if (storiesRead >= 1)       earned.Add("first_story");
            if (storiesRead >= 5)       earned.Add("story_explorer");
            if (lessonsCompleted >= 1)  earned.Add("first_lesson");
            if (lessonsCompleted >= 10) earned.Add("lesson_master");
            if (writingAccepted >= 1)   earned.Add("first_writing");
            if (writingAccepted >= 10)  earned.Add("calligraphy_star");
            if (stars >= 10)            earned.Add("star_collector");
            if (xp >= 100)              earned.Add("xp_100");
            if (xp >= 500)              earned.Add("xp_500");
            return earned;
        }
    }
}
