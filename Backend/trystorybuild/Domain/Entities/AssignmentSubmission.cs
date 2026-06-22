namespace Domain.Entities
{
    public class AssignmentSubmission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssignmentId { get; set; }
        public LessonAssignment Assignment { get; set; } = null!;
        public Guid StudentId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public int PagesCompleted { get; set; }
        public int TotalPages { get; set; }
        public double WritingScore { get; set; }
        public bool IsComplete { get; set; } = false;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string NotesJson { get; set; } = "{}";
    }
}
