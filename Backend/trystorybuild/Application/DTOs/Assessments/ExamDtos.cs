using Domain.Entities;

namespace Application.DTOs
{
    // ── Exam Response ──────────────────────────────────────────────────────────
    public record ExamResponse(Guid ExamId, Guid StoryId, List<QuestionDto> Questions);

    public record QuestionDto(
        Guid QuestionId,
        int QuestionNumber,
        QuizType Type,
        string Text,
        string? OptionA,
        string? OptionB,
        string? OptionC,
        string? OptionD,
        string? DataJson);

    // ── Internal AI Output ─────────────────────────────────────────────────────
    public record AiExamOutput(List<AiQuestion> Questions);

    public record AiQuestion(
        string Type,
        string Text,
        string? OptionA,
        string? OptionB,
        string? OptionC,
        string? OptionD,
        string? CorrectAnswer,
        List<AiMatchPair>? Pairs,
        string? Sentence,
        List<string>? Options,
        string? DragAnswer,
        List<string>? Words,
        List<string>? CorrectOrder);

    public record AiMatchPair(string Left, string Right);

    // ── Exam Submission ────────────────────────────────────────────────────────
    public record SubmitExamRequest(Guid ExamId, string ChildName, List<SubmitAnswer> Answers);

    public record SubmitAnswer(Guid QuestionId, string ChosenAnswer);

    public record ExamResultResponse(
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        List<AnswerFeedback> Feedback);

    public record AnswerFeedback(
        Guid QuestionId,
        QuizType Type,
        string ChosenAnswer,
        string CorrectAnswer,
        bool IsCorrect);
}
