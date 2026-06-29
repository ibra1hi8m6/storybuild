using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/quiz")]
public class GateQuizController(AppDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════
    //  GATE QUIZ 1  (Level 1 → 2)
    //  Format: 5 images, one at a time, 4 letter choices each
    //  Pass:   5 / 5 correct  →  level becomes 2
    //  Fail:   any wrong      →  all letter completions reset
    // ═══════════════════════════════════════════════════════════

    [HttpGet("gate1/{studentId:guid}")]
    public async Task<IActionResult> GetGateQuiz1(Guid studentId)
    {
        var student = await db.Students.FindAsync(studentId);
        if (student is null) return NotFound();
        if (student.Level != 1)
            return BadRequest(new { error = "هذا الاختبار للمستوى الأول فقط" });

        // Must have completed all published letters first
        var lettersTotal     = await db.LetterContents.CountAsync(l => l.IsPublished);
        var lettersCompleted = await db.StudentContentCompletions
            .CountAsync(c => c.StudentId == studentId && c.ContentType == ContentCompletionType.Letter);

        if (lettersCompleted < lettersTotal)
            return BadRequest(new { error = "يجب إتمام جميع الحروف أولاً" });

        // Pick 5 random letters that have images
        var lettersWithImages = await db.LetterContents
            .Where(l => l.IsPublished && l.ImagePath != null && l.ImagePath != "")
            .ToListAsync();

        if (lettersWithImages.Count < 5)
            return BadRequest(new { error = "لا توجد صور كافية للحروف" });

        var rng      = new Random();
        var selected = lettersWithImages.OrderBy(_ => rng.Next()).Take(5).ToList();
        var allLetterStrings = lettersWithImages.Select(l => l.Letter).Distinct().ToList();

        var questions = selected.Select((l, i) => new GateQuiz1QuestionDto(
            QuestionIndex: i + 1,
            LetterId:      l.Id,
            ImagePath:     l.ImagePath,
            Choices:       BuildChoices(l.Letter, allLetterStrings, rng)
        )).ToList();

        return Ok(new { questions });
    }

    [HttpPost("gate1/submit")]
    public async Task<IActionResult> SubmitGateQuiz1([FromBody] GateQuiz1SubmitRequest req)
    {
        var student = await db.Students.FindAsync(req.StudentId);
        if (student is null) return NotFound();
        if (student.Level != 1)
            return BadRequest(new { error = "هذا الاختبار للمستوى الأول فقط" });

        // Load the letters being tested so we can verify answers server-side
        var letterIds   = req.Answers.Select(a => a.LetterId).ToList();
        var letterLookup = await db.LetterContents
            .Where(l => letterIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Letter);

        int score = req.Answers
            .Count(a => letterLookup.TryGetValue(a.LetterId, out var correct)
                        && a.ChosenLetter == correct);

        bool passed = score == req.Answers.Count && req.Answers.Count == 5;

        if (passed)
        {
            student.Level = 2;
            await db.SaveChangesAsync();
        }
        else
        {
            // Reset — delete all letter completion rows for this student
            var toDelete = await db.StudentContentCompletions
                .Where(c => c.StudentId   == req.StudentId
                         && c.ContentType == ContentCompletionType.Letter)
                .ToListAsync();
            db.StudentContentCompletions.RemoveRange(toDelete);
            await db.SaveChangesAsync();
        }

        return Ok(new GateQuiz1ResultDto(passed, score, 5, Reset: !passed));
    }

    // ═══════════════════════════════════════════════════════════
    //  GATE QUIZ 2  (Level 2 → 3)
    //  Part A: write 5 random words   — evaluated by existing AI endpoint
    //  Part B: read  5 random sentences (fluency) — evaluated by AI
    //  Pass:   both parts 5/5         →  level becomes 3
    //  Fail:   either part wrong      →  words + sentences completions reset
    // ═══════════════════════════════════════════════════════════

    [HttpGet("gate2/{studentId:guid}")]
    public async Task<IActionResult> GetGateQuiz2(Guid studentId)
    {
        var student = await db.Students.FindAsync(studentId);
        if (student is null) return NotFound();
        if (student.Level != 2)
            return BadRequest(new { error = "هذا الاختبار للمستوى الثاني فقط" });

        // Must have completed all words AND all sentences
        var wordsTotal         = await db.WordContents.CountAsync(w => w.IsPublished);
        var sentencesTotal     = await db.SentenceContents.CountAsync(s => s.IsPublished);
        var wordsCompleted     = await db.StudentContentCompletions
            .CountAsync(c => c.StudentId == studentId && c.ContentType == ContentCompletionType.Word);
        var sentencesCompleted = await db.StudentContentCompletions
            .CountAsync(c => c.StudentId == studentId && c.ContentType == ContentCompletionType.Sentence);

        if (wordsCompleted < wordsTotal || sentencesCompleted < sentencesTotal)
            return BadRequest(new { error = "يجب إتمام جميع الكلمات والجمل أولاً" });

        var rng = new Random();

        // 5 random words
        var words = await db.WordContents
            .Where(w => w.IsPublished)
            .ToListAsync();
        var selectedWords = words.OrderBy(_ => rng.Next()).Take(5)
            .Select(w => new GateQuiz2WordDto(w.Id, w.DisplayWord, w.AudioText, w.ImagePath))
            .ToList();

        // 5 random sentences
        var sentences = await db.SentenceContents
            .Where(s => s.IsPublished)
            .ToListAsync();
        var selectedSentences = sentences.OrderBy(_ => rng.Next()).Take(5)
            .Select(s => new GateQuiz2SentenceDto(
                s.Id,
                s.CorrectOptionIndex == 1 ? s.Option1 :
                s.CorrectOptionIndex == 2 ? s.Option2 : s.Option3,
                s.CorrectOptionIndex == 1 ? s.Option1Audio :
                s.CorrectOptionIndex == 2 ? s.Option2Audio : s.Option3Audio))
            .ToList();

        return Ok(new { words = selectedWords, sentences = selectedSentences });
    }

    // Called by frontend after BOTH parts evaluated and passed
    [HttpPost("gate2/complete")]
    public async Task<IActionResult> CompleteGateQuiz2([FromBody] GateQuiz2CompleteRequest req)
    {
        var student = await db.Students.FindAsync(req.StudentId);
        if (student is null) return NotFound();
        if (student.Level != 2)
            return BadRequest(new { error = "هذا الاختبار للمستوى الثاني فقط" });

        if (req.Passed)
        {
            student.Level = 3;
            await db.SaveChangesAsync();
            return Ok(new { passed = true, reset = false });
        }
        else
        {
            // Reset — delete all word + sentence completions
            var toDelete = await db.StudentContentCompletions
                .Where(c => c.StudentId == req.StudentId
                         && (c.ContentType == ContentCompletionType.Word
                          || c.ContentType == ContentCompletionType.Sentence))
                .ToListAsync();
            db.StudentContentCompletions.RemoveRange(toDelete);
            await db.SaveChangesAsync();
            return Ok(new { passed = false, reset = true });
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private static List<string> BuildChoices(string correct, List<string> allLetters, Random rng)
    {
        var wrongs  = allLetters.Where(l => l != correct).OrderBy(_ => rng.Next()).Take(3).ToList();
        var choices = new List<string> { correct };
        choices.AddRange(wrongs);
        return choices.OrderBy(_ => rng.Next()).ToList();
    }
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record GateQuiz1QuestionDto(
    int          QuestionIndex,
    Guid         LetterId,
    string       ImagePath,
    List<string> Choices);

public record GateQuiz1SubmitRequest
{
    public Guid                   StudentId { get; init; }
    public List<GateQuiz1Answer>  Answers   { get; init; } = [];
}

public record GateQuiz1Answer(Guid LetterId, string ChosenLetter);

public record GateQuiz1ResultDto(bool Passed, int Score, int Total, bool Reset);

public record GateQuiz2WordDto(
    Guid    WordId,
    string  DisplayWord,
    string  AudioText,
    string? ImagePath);

public record GateQuiz2SentenceDto(
    Guid   SentenceId,
    string SentenceText,
    string AudioText);

public record GateQuiz2CompleteRequest(Guid StudentId, bool Passed);
