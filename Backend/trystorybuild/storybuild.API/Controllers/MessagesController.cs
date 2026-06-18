using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessagesController(IMessagingRepository repo, IAudioStorageService audioStorage) : ControllerBase
    {
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest req)
        {
            var type = Enum.TryParse<MessageType>(req.Type, true, out var t) ? t : MessageType.Text;
            var msg = await repo.SaveAsync(new Message
            {
                SenderId = req.SenderId,
                ReceiverId = req.ReceiverId,
                Content = req.Content,
                Type = type
            });
            return Ok(ToDto(msg, req.Type));
        }

        [HttpPost("send-voice")]
        public async Task<IActionResult> SendVoice([FromForm] Guid senderId, [FromForm] Guid receiverId, IFormFile audio)
        {
            var url = await audioStorage.UploadAudioAsync(audio, "lughati/voice-messages");
            var msg = await repo.SaveAsync(new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = url,
                Type = MessageType.Voice
            });
            return Ok(ToDto(msg, "Voice"));
        }

        [HttpGet("inbox/{userId:guid}")]
        public async Task<IActionResult> Inbox(Guid userId)
        {
            var messages = await repo.GetInboxAsync(userId);
            return Ok(messages.Select(m => ToDto(m, m.Type.ToString())));
        }

        [HttpGet("unread-count/{userId:guid}")]
        public async Task<IActionResult> UnreadCount(Guid userId)
        {
            var count = await repo.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPost("{messageId:guid}/mark-read/{userId:guid}")]
        public async Task<IActionResult> MarkRead(Guid messageId, Guid userId)
        {
            await repo.MarkReadAsync(messageId, userId);
            return NoContent();
        }

        private static MessageDto ToDto(Message m, string typeStr) =>
            new(m.Id, m.SenderId, string.Empty, m.ReceiverId, m.Content, typeStr, m.IsRead, m.CreatedAt);
    }
}
