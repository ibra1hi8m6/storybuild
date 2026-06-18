namespace Domain.Entities
{
    public class LessonVocabulary
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LessonId { get; set; }
        public string Word { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
