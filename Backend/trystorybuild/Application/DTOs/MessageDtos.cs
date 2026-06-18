namespace Application.DTOs
{
    public record MessageDto(
        Guid Id,
        Guid SenderId,
        string SenderName,
        Guid ReceiverId,
        string Content,
        string Type,
        bool IsRead,
        DateTime CreatedAt
    );

    public record SendMessageRequest(
        Guid SenderId,
        Guid ReceiverId,
        string Content,
        string Type
    );

    public record SendVoiceMessageRequest(
        Guid SenderId,
        Guid ReceiverId
    );
}
