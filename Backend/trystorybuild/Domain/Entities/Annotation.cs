namespace Domain.Entities
{
    public enum AnnotationType { Highlight = 0, Stamp = 1 }

    public class Annotation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public Guid PageId { get; set; }
        public string PageType { get; set; } = "Story"; // "Story" | "Lesson"
        public AnnotationType Type { get; set; } = AnnotationType.Highlight;
        public string ColorHex { get; set; } = "#FEF08A";
        public string SelectedText { get; set; } = string.Empty;
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
