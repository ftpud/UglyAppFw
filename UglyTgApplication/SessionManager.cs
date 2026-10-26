using System.Collections.Concurrent;
using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication.Entity;

namespace UglyTgApplication;

[Managed]
public class SessionManager
{
    private readonly ConcurrentDictionary<(long ChatId, long UserId), SessionContext> _sessions = new();

    public SessionContext GetCurrentContext(long chatId, long userId) =>
        _sessions.GetOrAdd((chatId, userId), key => new SessionContext
        {
            CurrentChatId = key.ChatId,
            CurrentUserId = key.UserId
        });

    [Obsolete("Use GetCurrentContext(chatId, userId). This overload assumes a private chat.")]
    public SessionContext getCurrentContext(long telegramUserId) =>
        GetCurrentContext(telegramUserId, telegramUserId);

    // Keep the context and its lock so queued updates cannot split into separate sessions.
    public async Task ResetSessionAsync(long chatId, long userId, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue((chatId, userId), out var context)) return;
        await context.UpdateLock.WaitAsync(cancellationToken);
        try
        {
            while (context.CurrentState != null)
            {
                var current = context.CurrentState;
                await current.UnloadAsync(cancellationToken);
                context.SetCurrentState(current.GetParentState());
            }
        }
        finally { context.UpdateLock.Release(); }
    }
}
