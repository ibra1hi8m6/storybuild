namespace Domain.Entities
{
    public class LetterContent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Letter { get; set; } = string.Empty;
        public string LetterName { get; set; } = string.Empty;
        public string ExampleWord { get; set; } = string.Empty;
        public string DisplaySentence { get; set; } = string.Empty;
        public string AudioText { get; set; } = string.Empty;
        public string WritingTarget { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
