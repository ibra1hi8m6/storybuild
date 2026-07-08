namespace Domain.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public SubscriptionPlan Plan { get; set; }
        public DateTime StartsAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int? MaxTeachers { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum SubscriptionPlan
    {
        Free           = 0,
        ParentPremium  = 1,
        ParentFamily   = 2,
        TeacherFree    = 3,
        TeacherPremium = 4,
        SchoolTrial    = 5,
        SchoolPremium  = 6,
        DemoFullAccess = 7,
    }
}
