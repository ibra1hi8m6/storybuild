namespace Domain.Entities
{
    public class WordContent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DisplayWord { get; set; } = string.Empty;
        public string AudioText { get; set; } = string.Empty;
        public string RelatedLetter { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
