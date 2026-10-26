using System.Collections.Concurrent;
using UglyTgApplication.Entity;

namespace UglyTgApplication.Services;

public class InMemoryUiMessageStore : IUiMessageStore
{
    private readonly ConcurrentDictionary<(long BotId, long ChatId, long UserId),
        ConcurrentDictionary<int, StoredUiMessage>> _messages = new();

    public Task<IReadOnlyList<StoredUiMessage>> GetAsync(long botId, long chatId, long userId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        IReadOnlyList<StoredUiMessage> result = _messages.TryGetValue((botId, chatId, userId), out var messages)
            ? messages.Values.ToArray() : Array.Empty<StoredUiMessage>();
        return Task.FromResult(result);
    }

    public Task AddAsync(long botId, long chatId, long userId, StoredUiMessage message, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        _messages.GetOrAdd((botId, chatId, userId), _ => new())[message.MessageId] = message;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(long botId, long chatId, long userId, int messageId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (_messages.TryGetValue((botId, chatId, userId), out var messages)) messages.TryRemove(messageId, out _);
        return Task.CompletedTask;
    }
}
