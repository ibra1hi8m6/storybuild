namespace Domain.Entities
{
    public enum ContentCompletionType
    {
        Letter   = 1,
        Word     = 2,
        Sentence = 3,
        Lesson   = 4,
        Story    = 5
    }

    public class StudentContentCompletion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public ContentCompletionType ContentType { get; set; }
        public Guid ContentId { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public Student? Student { get; set; }
    }
}
