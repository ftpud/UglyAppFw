using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.StateManage;

[Managed]
public abstract class State : IState
{
    [Inject] protected StateManager _stateManager{ get; set; }
    
    protected IState ParentState { get; set; }
    
    public void Push(IState state)
    {
        _stateManager.PushState(GetContext(), state);
    }

    public void Pop()
    {
        _stateManager.PopState(GetContext());
    }

    public virtual void Initialize()
    {
        throw new NotImplementedException();
    }
    
    public virtual void Unload()
    {
        throw new NotImplementedException();
    }

    public virtual void SetContext(ISessionContext context)
    {
        throw new NotImplementedException();
    }

    public virtual ISessionContext GetContext()
    {
        throw new NotImplementedException();
    }

    public IState GetParentState()
    {
        return ParentState;
    }

    public void SetParentState(IState state)
    {
        ParentState = state;
    }
}