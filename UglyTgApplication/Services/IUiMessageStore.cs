using UglyTgApplication.Entity;

namespace UglyTgApplication.Services;

public interface IUiMessageStore
{
    Task<IReadOnlyList<StoredUiMessage>> GetAsync(long botId, long chatId, long userId, CancellationToken token = default);
    Task AddAsync(long botId, long chatId, long userId, StoredUiMessage message, CancellationToken token = default);
    Task RemoveAsync(long botId, long chatId, long userId, int messageId, CancellationToken token = default);
}
