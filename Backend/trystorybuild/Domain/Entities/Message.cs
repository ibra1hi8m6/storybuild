namespace Domain.Entities
{
    public enum MessageType { Text = 0, Voice = 1, Sticker = 2 }

    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty; // text or cloudinary url for voice
        public MessageType Type { get; set; } = MessageType.Text;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
