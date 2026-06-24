namespace Application.Interfaces
{
    // ── Email Service ──────────────────────────────────────────────────────────
    public interface IEmailService
    {
        Task SendTeacherWelcomeAsync(string toEmail, string teacherName, string password);
        Task SendTeacherPasswordResetAsync(string toEmail, string teacherName, string newPassword);
    }
}
