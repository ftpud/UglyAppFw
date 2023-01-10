using Telegram.Bot.Types;

namespace TgUI.Entity;

public class SessionContext
{
    public State? CurrentState { get; set; }
    public ChatId CurrentUserId { get; set; }
    public Object CustomSessionData { get; set; }
    
}