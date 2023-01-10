using Telegram.Bot.Types;
using TgUI.Entity;

namespace TgUI.States;

public class State
{

    // Public properties 
    public bool RemoveUserMessagesPolicyEnabled { get; set; } = true;
    
    public State? ParentState { get; set; }
    protected SessionContext Context { get; set; }
    internal StateManager _stateManager { get; set; }
    
    internal int[] MessageId { get; set; }
    
    public virtual void Initialize()
    {
        
    }

    public virtual void Update(Update update)
    {
        
    }

    internal void SetContext(SessionContext context)
    {
        Context = context;
    }

    protected void Push(State newState)
    {
        _stateManager.PushState(Context, newState);
    }

    protected void Pop()
    {
        _stateManager.PopState(Context);
    }
}