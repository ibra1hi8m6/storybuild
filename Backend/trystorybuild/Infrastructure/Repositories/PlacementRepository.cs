using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Repositories
{
    public class PlacementRepository(AppDbContext db) : IPlacementRepository
    {
        public async Task<List<PlacementQuestion>> GetAllAsync() =>
            await db.PlacementQuestions
                .OrderBy(q => q.Part)
                .ThenBy(q => q.Order)
                .ToListAsync();

        public async Task<List<PlacementQuestion>> GetByPartAsync(int part) =>
            await db.PlacementQuestions
                .Where(q => q.Part == part)
                .OrderBy(q => q.Order)
                .ToListAsync();

        public async Task SeedAsync()
        {
            // Re-seed if Part 1 Q5 still has the old format (sentence in questionText, not imageContent)
            var q1p5 = await db.PlacementQuestions
                .Where(q => q.Part == 1 && q.Order == 5)
                .FirstOrDefaultAsync();

            if (q1p5?.QuestionText == "ما هذا؟") return; // Already up to date

            db.PlacementQuestions.RemoveRange(db.PlacementQuestions);
            await db.SaveChangesAsync();

            var questions = new List<PlacementQuestion>
            {
                // ═══════════════════════════════════════════════════════
                //  الجزء الأول: التمييز بين (حرف – كلمة – جملة) - 5 أسئلة
                // ═══════════════════════════════════════════════════════
                new() { Part=1, Order=1, QuestionText="ما هذا؟", ImageContent="أ",
                    AudioText="ما هذا؟", CorrectAnswer="A",
                    OptionsJson=Opts3("حرف","كلمة","جملة") },
                new() { Part=1, Order=2, QuestionText="ما هذا؟", ImageContent="قطة",
                    AudioText="ما هذا؟", CorrectAnswer="B",
                    OptionsJson=Opts3("حرف","كلمة","جملة") },
                new() { Part=1, Order=3, QuestionText="ما هذا؟", ImageContent="ذهب أخي إلى المدرسة.",
                    AudioText="ما هذا؟", CorrectAnswer="C",
                    OptionsJson=Opts3("حرف","كلمة","جملة") },
                new() { Part=1, Order=4, QuestionText="ما هذا؟", ImageContent="ق",
                    AudioText="ما هذا؟", CorrectAnswer="A",
                    OptionsJson=Opts3("حرف","كلمة","جملة") },
                new() { Part=1, Order=5, QuestionText="ما هذا؟", ImageContent="الولد يلعب بالكرة.",
                    AudioText="ما هذا؟", CorrectAnswer="C",
                    OptionsJson=Opts3("حرف","كلمة","جملة") },

                // ═══════════════════════════════════════════════════════
                //  الجزء الثاني: التعرف على الحرف وصوته - 5 أسئلة (صوتي فقط)
                // ═══════════════════════════════════════════════════════
                new() { Part=2, Order=1, QuestionText="أي حرف تسمعه؟", ImageContent="",
                    AudioText="ألف", CorrectAnswer="B",
                    OptionsJson=Opts("ب","أ","ت","ث") },
                new() { Part=2, Order=2, QuestionText="أي حرف تسمعه؟", ImageContent="",
                    AudioText="باء", CorrectAnswer="A",
                    OptionsJson=Opts("ب","ت","ث","ن") },
                new() { Part=2, Order=3, QuestionText="أي حرف تسمعه؟", ImageContent="",
                    AudioText="تاء", CorrectAnswer="B",
                    OptionsJson=Opts("ب","ت","ث","ن") },
                new() { Part=2, Order=4, QuestionText="أي حرف تسمعه؟", ImageContent="",
                    AudioText="ثاء", CorrectAnswer="C",
                    OptionsJson=Opts("ب","ت","ث","ن") },
                new() { Part=2, Order=5, QuestionText="أي حرف تسمعه؟", ImageContent="",
                    AudioText="جيم", CorrectAnswer="D",
                    OptionsJson=Opts("ح","خ","ع","ج") },

                // ═══════════════════════════════════════════════════════
                //  الجزء الثالث: تكوين جملة بسيطة - 5 أسئلة
                // ═══════════════════════════════════════════════════════
                new() { Part=3, Order=1, QuestionText="رتب الكلمات: يلعب – الولد – الكرة", ImageContent="🏃‍♂️⚽",
                    AudioText="رتب الكلمات: يلعب، الولد، الكرة", CorrectAnswer="A",
                    OptionsJson=Opts("الولد يلعب الكرة","يلعب الولد الكرة","الكرة يلعب الولد","يلعب الكرة الولد") },
                new() { Part=3, Order=2, QuestionText="رتب الكلمات: القطة – تشرب – الحليب", ImageContent="🐱🥛",
                    AudioText="رتب الكلمات: القطة، تشرب، الحليب", CorrectAnswer="B",
                    OptionsJson=Opts("تشرب القطة الحليب","القطة تشرب الحليب","الحليب تشرب القطة","القطة الحليب تشرب") },
                new() { Part=3, Order=3, QuestionText="أكمل الجملة: الشمس ___ في السماء.", ImageContent="☀️🌳",
                    AudioText="أكمل الجملة: الشمس في السماء", CorrectAnswer="B",
                    OptionsJson=Opts("تنام","تشرق","تسبح","تطير") },
                new() { Part=3, Order=4, QuestionText="أكمل الجملة: الفراشة ___ بين الزهور.", ImageContent="🦋🌸",
                    AudioText="أكمل الجملة: الفراشة بين الزهور", CorrectAnswer="C",
                    OptionsJson=Opts("تسبح","تنام","تطير","تجري") },
                new() { Part=3, Order=5, QuestionText="أكمل الجملة: الولد ___ القصة.", ImageContent="📖👦",
                    AudioText="أكمل الجملة: الولد القصة", CorrectAnswer="A",
                    OptionsJson=Opts("يقرأ","يأكل","يلعب","ينام") },
            };

            db.PlacementQuestions.AddRange(questions);
            await db.SaveChangesAsync();
        }

        // 3-option helper for Part 1 (حرف / كلمة / جملة)
        private static string Opts3(string a, string b, string c) =>
            JsonSerializer.Serialize(new[]
            {
                new { key = "A", emoji = "", label = a },
                new { key = "B", emoji = "", label = b },
                new { key = "C", emoji = "", label = c }
            });

        private static string Opts(string a, string b, string c, string d) =>
            JsonSerializer.Serialize(new[]
            {
                new { key = "A", emoji = "", label = a },
                new { key = "B", emoji = "", label = b },
                new { key = "C", emoji = "", label = c },
                new { key = "D", emoji = "", label = d }
            });
    }
}
