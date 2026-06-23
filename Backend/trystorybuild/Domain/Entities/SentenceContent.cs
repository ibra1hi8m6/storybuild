namespace Domain.Entities
{
    public class SentenceContent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ImagePath { get; set; } = string.Empty;
        public string Option1 { get; set; } = string.Empty;
        public string Option1Audio { get; set; } = string.Empty;
        public string Option2 { get; set; } = string.Empty;
        public string Option2Audio { get; set; } = string.Empty;
        public string Option3 { get; set; } = string.Empty;
        public string Option3Audio { get; set; } = string.Empty;
        public int CorrectOptionIndex { get; set; } = 1;
        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
