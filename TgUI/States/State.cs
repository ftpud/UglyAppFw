using Telegram.Bot.Types;

namespace TgUI.Entity;

public class State
{
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