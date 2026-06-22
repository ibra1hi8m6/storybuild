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

            // Get the school admin's school code to link teacher to school
            var schoolAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == "school@lughati.com");
            var schoolCode  = schoolAdmin?.Id.ToString("N")[..8].ToUpper() ?? "TESTSCHOOL";

            db.Teachers.Add(new Teacher
            {
                Id         = teacherUserId,
                User       = teacherUser,
                SchoolCode = schoolCode,
                IsPrivate  = false,
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
    }
}
