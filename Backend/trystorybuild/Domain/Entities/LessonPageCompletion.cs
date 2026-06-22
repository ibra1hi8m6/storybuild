namespace Domain.Entities
{
    public class LessonPageCompletion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ChildName { get; set; } = "";
        public Guid LessonId { get; set; }
        public Guid LessonPageId { get; set; }
        public bool WritingSubmitted { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
