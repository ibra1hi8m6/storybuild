namespace Domain.Entities
{
    public class SubscriptionActivationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public SubscriptionPlan Plan { get; set; }
        public int DurationDays { get; set; }
        public int MaxUses { get; set; } = 1;
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }
        public string? Notes { get; set; }
    }
}
