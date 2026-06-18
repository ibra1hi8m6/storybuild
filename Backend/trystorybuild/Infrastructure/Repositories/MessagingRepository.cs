using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class MessagingRepository(AppDbContext db) : IMessagingRepository
    {
        public async Task<Message> SaveAsync(Message message)
        {
            db.Messages.Add(message);
            await db.SaveChangesAsync();
            return message;
        }

        public async Task<List<Message>> GetInboxAsync(Guid userId) =>
            await db.Messages
                .Where(m => m.ReceiverId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

        public async Task<int> GetUnreadCountAsync(Guid userId) =>
            await db.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);

        public async Task MarkReadAsync(Guid messageId, Guid userId)
        {
            var msg = await db.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);
            if (msg is null) return;
            msg.IsRead = true;
            await db.SaveChangesAsync();
        }
    }
}
