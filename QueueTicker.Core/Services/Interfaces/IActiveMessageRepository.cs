using QueueTicker.Core.Models;

namespace QueueTicker.Core.Services.Interfaces
{
    public interface IActiveMessageRepository
    {
        Task CreateActiveMessage(ActiveMessage activeMessage);
        Task<bool> DeleteActiveMessage(ActiveMessage activeMessage);
        Task<List<ActiveMessage>> GetActiveMessages();
        Task<List<ActiveMessage>> GetActiveMessages(ulong serverId);
    }
}