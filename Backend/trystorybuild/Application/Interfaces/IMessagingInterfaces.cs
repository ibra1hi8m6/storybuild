using Domain.Entities;

namespace Application.Interfaces
{
    public interface IMessagingRepository
    {
        Task<Message> SaveAsync(Message message);
        Task<List<Message>> GetInboxAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkReadAsync(Guid messageId, Guid userId);
    }
}
