namespace Domain.Entities
{
    public enum ContentStatus
    {
        Draft     = 0,
        Reviewed  = 1,
        Embedded  = 2,
        Published = 3,
        Failed    = 4
    }

    public class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Level { get; set; }
        public string Letter { get; set; } = string.Empty;
        public string LetterName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Content lifecycle
        public bool IsPublished { get; set; } = true;
        public ContentStatus Status { get; set; } = ContentStatus.Published;

        // Generation metadata
        public Guid? CreatorId { get; set; }
        public string CreatorRole { get; set; } = "Admin";
        public bool IsGenerated { get; set; } = false;
        public string PromptText { get; set; } = string.Empty;

        public List<LessonPage> Pages { get; set; } = new();
        public List<LessonAssignment> Assignments { get; set; } = new();
    }
}
