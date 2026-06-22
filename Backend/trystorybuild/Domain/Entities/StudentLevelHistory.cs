namespace Domain.Entities
{
    public class StudentLevelHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public int PreviousLevel { get; set; }
        public int NewLevel { get; set; }
        public Guid ChangedByUserId { get; set; }
        public string ChangedByRole { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
