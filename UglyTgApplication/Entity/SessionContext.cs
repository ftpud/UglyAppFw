using Telegram.Bot.Types;
using UglyAppFramework.StateManage;
using UglyTgApplication.States;

namespace UglyTgApplication.Entity;

public class SessionContext : ISessionContext
{
    public TgState? CurrentState { get; set; }
    // Sender and destination are distinct, particularly in group chats.
    public ChatId CurrentUserId { get; set; } = null!;
    public ChatId CurrentChatId { get; set; } = null!;
    public long UserId => CurrentUserId.Identifier!.Value;
    internal SemaphoreSlim UpdateLock { get; } = new(1, 1);

    public IState? GetCurrentState() => CurrentState;
    public void SetCurrentState(IState? state) => CurrentState = (TgState?)state;
}
