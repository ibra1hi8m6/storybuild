using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // ── SystemAdmin ───────────────────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.SystemAdmin))
        {
            db.Users.Add(new User
            {
                Name         = "System Admin",
                Email        = "admin@lughati.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@Lughati2026"),
                Role         = UserRole.SystemAdmin,
                IsActive     = true,
            });
            await db.SaveChangesAsync();
        }

        // ── SchoolAdmin test account ───────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "school@lughati.com"))
        {
            var schoolAdmin = new User
            {
                Name         = "مدير المدرسة النموذجية",
                Email        = "school@lughati.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("School@2026"),
                Role         = UserRole.SchoolAdmin,
                IsActive     = true,
            };
            db.Users.Add(schoolAdmin);
            await db.SaveChangesAsync();
        }

        // ── Teacher test account ──────────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "teacher@lughati.com"))
        {
            var teacherUserId = Guid.NewGuid();
            var teacherUser = new User
            {
                Id           = teacherUserId,
                Name         = "معلمة لغة عربية",
                Email        = "teacher@lughati.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@2026"),
                Role         = UserRole.Teacher,
                IsActive     = true,
            };
            db.Users.Add(teacherUser);
            await db.SaveChangesAsync();

            // Link teacher to school admin by SchoolManagerId
            var schoolAdmin     = await db.Users.FirstOrDefaultAsync(u => u.Email == "school@lughati.com");
            var schoolManagerId = schoolAdmin?.Id;

            db.Teachers.Add(new Teacher
            {
                Id             = teacherUserId,
                User           = teacherUser,
                SchoolManagerId = schoolManagerId,
                IsPrivate      = !schoolManagerId.HasValue,
            });
            await db.SaveChangesAsync();
        }

        // ── Parent test account ──────────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "parent@lughati.com"))
        {
            var parentUserId = Guid.NewGuid();
            var parentUser = new User
            {
                Id           = parentUserId,
                Name         = "ولي الأمر",
                Email        = "parent@lughati.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent@2026"),
                Role         = UserRole.Parent,
                IsActive     = true,
            };
            db.Users.Add(parentUser);
            await db.SaveChangesAsync();

            db.Parents.Add(new Parent
            {
                Id   = parentUserId,
                User = parentUser,
            });
            await db.SaveChangesAsync();
        }

        // ── Test parent for gate-quiz testing ────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "testparent@lughati.com"))
        {
            var testParentId = Guid.NewGuid();
            var testParentUser = new User
            {
                Id           = testParentId,
                Name         = "ولي أمر تجريبي",
                Email        = "testparent@lughati.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestParent@2026"),
                Role         = UserRole.Parent,
                IsActive     = true,
            };
            db.Users.Add(testParentUser);
            await db.SaveChangesAsync();

            db.Parents.Add(new Parent { Id = testParentId, User = testParentUser });
            await db.SaveChangesAsync();
        }

        var testParent = await db.Users.FirstAsync(u => u.Email == "testparent@lughati.com");

        // ── Test child 1: Level 1 — all letters done → Gate Quiz 1 unlocks ───────
        if (!await db.Students.AnyAsync(s => s.NationalId == "SEED-TEST-L1"))
        {
            var child1 = new Student
            {
                Name          = "زياد",
                Age           = 8,
                Username      = "ziyad",
                NationalId    = "SEED-TEST-L1",
                ImagePin1     = 3,   // 🐟 سمكة
                ImagePin2     = null,
                Level         = 1,
                PlacementDone = true,
                AvatarEmoji   = "🐟",
                LoginMethod   = StudentLoginMethod.ImagePin,
                ParentId      = testParent.Id,
            };
            db.Students.Add(child1);
            await db.SaveChangesAsync();

            var letterIds = await db.LetterContents
                .Where(l => l.IsPublished)
                .Select(l => l.Id)
                .ToListAsync();

            db.StudentContentCompletions.AddRange(letterIds.Select(lid => new StudentContentCompletion
            {
                StudentId   = child1.Id,
                ContentType = ContentCompletionType.Letter,
                ContentId   = lid,
                CompletedAt = DateTime.UtcNow,
            }));
            await db.SaveChangesAsync();
        }

        // ── Test child 2: Level 2 — all words + sentences done → Gate Quiz 2 unlocks
        if (!await db.Students.AnyAsync(s => s.NationalId == "SEED-TEST-L2"))
        {
            var child2 = new Student
            {
                Name          = "لينا",
                Age           = 9,
                Username      = "lina",
                NationalId    = "SEED-TEST-L2",
                ImagePin1     = 3,   // 🐟 سمكة
                ImagePin2     = null,
                Level         = 2,
                PlacementDone = true,
                AvatarEmoji   = "🐟",
                LoginMethod   = StudentLoginMethod.ImagePin,
                ParentId      = testParent.Id,
            };
            db.Students.Add(child2);
            await db.SaveChangesAsync();

            var wordIds = await db.WordContents
                .Where(w => w.IsPublished)
                .Select(w => w.Id)
                .ToListAsync();

            var sentenceIds = await db.SentenceContents
                .Where(s => s.IsPublished)
                .Select(s => s.Id)
                .ToListAsync();

            db.StudentContentCompletions.AddRange(wordIds.Select(wid => new StudentContentCompletion
            {
                StudentId   = child2.Id,
                ContentType = ContentCompletionType.Word,
                ContentId   = wid,
                CompletedAt = DateTime.UtcNow,
            }));

            db.StudentContentCompletions.AddRange(sentenceIds.Select(sid => new StudentContentCompletion
            {
                StudentId   = child2.Id,
                ContentType = ContentCompletionType.Sentence,
                ContentId   = sid,
                CompletedAt = DateTime.UtcNow,
            }));

            await db.SaveChangesAsync();
        }

        await BackfillCompletionsAsync(db);
        await BackfillClassroomStudentsAsync(db);
        await ClampCorruptedScoresAsync(db);
        await RemoveAiStoryCompletionsAsync(db);
        await SeedSubscriptionsAsync(db);
    }

    // Copies successful LearningAttempt records into StudentContentCompletions.
    // Idempotent — skips any (StudentId, ContentType, ContentId) triple already present.
    // Runs every startup but is a no-op once all rows are migrated.
    private static async Task BackfillCompletionsAsync(AppDbContext db)
    {
        // Map LearningContentType → ContentCompletionType
        static ContentCompletionType? Map(LearningContentType t) => t switch
        {
            LearningContentType.LetterSound       => ContentCompletionType.Letter,
            LearningContentType.LetterRecognition => ContentCompletionType.Letter,
            LearningContentType.WordPractice      => ContentCompletionType.Word,
            LearningContentType.SentencePractice  => ContentCompletionType.Sentence,
            _                                     => null
        };

        // Collect all successful attempts that have a StudentId
        var attempts = await db.LearningAttempts
            .Where(a => a.IsCorrect && a.StudentId != null)
            .Select(a => new { a.StudentId, a.ContentType, a.ContentId, a.CreatedAt })
            .ToListAsync();

        if (attempts.Count == 0) return;

        // Existing completions as a HashSet of tuples for O(1) lookup
        var existing = (await db.StudentContentCompletions
            .Select(c => new { c.StudentId, c.ContentType, c.ContentId })
            .ToListAsync())
            .Select(c => (c.StudentId, c.ContentType, c.ContentId))
            .ToHashSet();

        var toAdd = new List<StudentContentCompletion>();

        foreach (var a in attempts)
        {
            var completionType = Map(a.ContentType);
            if (completionType is null) continue;

            var sid = a.StudentId!.Value;
            var key = (sid, completionType.Value, a.ContentId);
            if (!existing.Add(key)) continue; // Add returns false if already present

            toAdd.Add(new StudentContentCompletion
            {
                StudentId   = sid,
                ContentType = completionType.Value,
                ContentId   = a.ContentId,
                CompletedAt = a.CreatedAt,
            });
        }

        if (toAdd.Count > 0)
        {
            db.StudentContentCompletions.AddRange(toAdd);
            await db.SaveChangesAsync();
            Console.WriteLine($"[Backfill] Inserted {toAdd.Count} completion records into StudentContentCompletions.");
        }
        else
        {
            Console.WriteLine("[Backfill] StudentContentCompletions already up to date — nothing to insert.");
        }
    }

    // Enrolls existing teacher-linked students into their teacher's classroom if missing.
    // Fixes students created before auto-enrollment was added.
    private static async Task BackfillClassroomStudentsAsync(AppDbContext db)
    {
        var teacherStudents = await db.Students
            .Where(s => s.TeacherId != null)
            .Select(s => new { s.Id, s.TeacherId })
            .ToListAsync();

        if (teacherStudents.Count == 0) return;

        var existingEnrollments = (await db.ClassroomStudents
            .Select(cs => cs.StudentId)
            .ToListAsync())
            .ToHashSet();

        var classrooms = await db.Classrooms
            .Select(c => new { c.Id, c.TeacherId })
            .ToListAsync();

        var classroomByTeacher = classrooms
            .GroupBy(c => c.TeacherId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var toAdd = new List<ClassroomStudent>();
        foreach (var s in teacherStudents)
        {
            if (existingEnrollments.Contains(s.Id)) continue;
            if (!classroomByTeacher.TryGetValue(s.TeacherId!.Value, out var classroomId)) continue;
            toAdd.Add(new ClassroomStudent { ClassroomId = classroomId, StudentId = s.Id });
        }

        if (toAdd.Count > 0)
        {
            db.ClassroomStudents.AddRange(toAdd);
            await db.SaveChangesAsync();
            Console.WriteLine($"[Backfill] Enrolled {toAdd.Count} student(s) into their teacher's classroom.");
        }
        else
        {
            Console.WriteLine("[Backfill] ClassroomStudents already up to date — nothing to insert.");
        }
    }

    // Removes StudentContentCompletion records for AI-generated stories.
    // AI stories (Source=AiGenerated) must not count as progress.
    // Only uploaded PDF stories (Source=PdfImport) should appear in parent/teacher dashboards.
    private static async Task RemoveAiStoryCompletionsAsync(AppDbContext db)
    {
        var aiStoryIds = await db.Stories
            .Where(s => s.Source == StorySource.AiGenerated)
            .Select(s => s.Id)
            .ToListAsync();

        if (aiStoryIds.Count == 0) return;

        var removed = await db.StudentContentCompletions
            .Where(c => c.ContentType == ContentCompletionType.Story
                     && aiStoryIds.Contains(c.ContentId))
            .ExecuteDeleteAsync();

        if (removed > 0)
            Console.WriteLine($"[Cleanup] Removed {removed} AI-story completion record(s) from StudentContentCompletions.");
        else
            Console.WriteLine("[Cleanup] No AI-story completions found — nothing to remove.");
    }

    // Marks seeded demo users as IsDemo = true and ensures each has a DemoFullAccess
    // subscription. Idempotent — safe to run on every startup.
    private static async Task SeedSubscriptionsAsync(AppDbContext db)
    {
        var demoEmails = new[]
        {
            "admin@lughati.com",
            "school@lughati.com",
            "teacher@lughati.com",
            "parent@lughati.com",
            "testparent@lughati.com",
        };

        bool anyChange = false;

        foreach (var email in demoEmails)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null) continue;

            // Mark as demo if not already
            if (!user.IsDemo)
            {
                user.IsDemo = true;
                anyChange = true;
            }

            // Seed DemoFullAccess subscription if missing
            var hasSub = await db.Subscriptions.AnyAsync(
                s => s.UserId == user.Id && s.Plan == SubscriptionPlan.DemoFullAccess);

            if (!hasSub)
            {
                db.Subscriptions.Add(new Subscription
                {
                    UserId    = user.Id,
                    Plan      = SubscriptionPlan.DemoFullAccess,
                    StartsAt  = DateTime.UtcNow,
                    ExpiresAt = null,   // never expires
                    IsActive  = true,
                });
                anyChange = true;
            }
        }

        if (anyChange)
        {
            await db.SaveChangesAsync();
            Console.WriteLine("[Subscription] Demo users marked and DemoFullAccess subscriptions seeded.");
        }
        else
        {
            Console.WriteLine("[Subscription] Demo subscriptions already up to date — nothing to seed.");
        }
    }

    // Clamps LearningAttempt.Score and WritingAttempt.SimilarityScore to [0, 100].
    // Fixes corrupted records written before the Gemini clamp guard was in place.
    // Idempotent — runs every startup but only touches rows that need fixing.
    private static async Task ClampCorruptedScoresAsync(AppDbContext db)
    {
        var badAttempts = await db.LearningAttempts
            .Where(a => a.Score < 0 || a.Score > 100)
            .ToListAsync();

        foreach (var a in badAttempts)
            a.Score = Math.Clamp(a.Score, 0, 100);

        var badWriting = await db.WritingAttempts
            .Where(w => w.SimilarityScore < 0 || w.SimilarityScore > 100)
            .ToListAsync();

        foreach (var w in badWriting)
            w.SimilarityScore = Math.Clamp(w.SimilarityScore, 0, 100);

        if (badAttempts.Count > 0 || badWriting.Count > 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"[Cleanup] Clamped {badAttempts.Count} LearningAttempt score(s) and {badWriting.Count} WritingAttempt score(s) to [0, 100].");
        }
        else
        {
            Console.WriteLine("[Cleanup] All scores already within [0, 100] — nothing to fix.");
        }
    }
}
