using Telegram.Bot.Types;
using UglyAppFramework.StateManage;
using UglyTgApplication.States;

namespace UglyTgApplication.Entity;

public class SessionContext : ISessionContext
{
    public TgState CurrentState { get; set; }
    public ChatId CurrentUserId { get; set; }
    

    public IState GetCurrentState()
    {
        return CurrentState;
    }

    public void SetCurrentState(IState state)
    {
        CurrentState = (TgState)state;
    }
}