namespace Application.DTOs
{
    // ── Adult auth ─────────────────────────────────────────────────────────────
    public record RegisterRequest(
        string FullName,
        string Email,
        string Password,
        string Role,
        Guid? SchoolManagerId = null);

    public record LoginRequest(string Email, string Password);

    public record AuthResponse(
        string Token,
        string UserId,
        string Name,
        string Role,
        DateTime ExpiresAt,
        Guid? SchoolManagerId = null);

    // ── Student management ─────────────────────────────────────────────────────
    public record CreateStudentRequest(
        string  Name,
        int     Age,
        string  Username,
        string  NationalId,
        int     ImagePin1,
        int?    ImagePin2   = null,
        int     Level       = 1,
        string? AvatarEmoji = null);

    public record StudentLoginRequest(
        string Username,
        int    ImagePin1,
        int?   ImagePin2 = null);

    public record StudentAuthResponse(
        string   Token,
        string   StudentId,
        string   Name,
        int      Level,
        bool     PlacementDone,
        DateTime ExpiresAt,
        string?  AvatarEmoji = null);

    public record StudentProfileDto(
        Guid    Id,
        string  Name,
        int     Age,
        string  Username,
        string? NationalId,
        int     Level,
        bool    PlacementDone,
        string? AvatarUrl,
        string? AvatarEmoji = null);
}
