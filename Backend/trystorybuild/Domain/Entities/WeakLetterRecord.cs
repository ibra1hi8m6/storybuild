namespace Domain.Entities
{
    public class WeakLetterRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public string Letter { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public int Correct { get; set; }
        public string ActivityType { get; set; } = "Writing"; // Writing | Reading | Exam
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    }
}
