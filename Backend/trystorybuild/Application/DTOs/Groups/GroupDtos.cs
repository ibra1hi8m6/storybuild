namespace Application.DTOs
{
    // ── Student Groups ─────────────────────────────────────────────────────────
    public record StudentGroupDto(
        Guid Id,
        string Name,
        Guid TeacherId,
        int MemberCount,
        DateTime CreatedAt,
        List<StudentGroupMemberDto> Members);

    public record StudentGroupMemberDto(Guid StudentId, string StudentName, DateTime AddedAt);

    public record CreateGroupRequest(string Name);

    public record AddGroupMemberRequest(Guid StudentId);

    public record AddDirectStudentRequest(string Identifier); // username or nationalId

    // ── Lesson Assignments ─────────────────────────────────────────────────────
    public record AssignLessonRequest(
        Guid LessonId,
        string TargetType,
        Guid? TargetStudentId,
        Guid? TargetGroupId);

    public record LessonAssignmentDto(
        Guid Id,
        Guid LessonId,
        string LessonTitle,
        string TargetType,
        Guid? TargetStudentId,
        string? TargetStudentName,
        Guid? TargetGroupId,
        string? TargetGroupName,
        DateTime AssignedAt);

    // ── Level Word Config ──────────────────────────────────────────────────────
    public record LevelWordConfigDto(int Level, int WordCount, string ExampleSentence);
}
