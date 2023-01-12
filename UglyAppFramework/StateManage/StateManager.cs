using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.StateManage;

[Managed]
public class StateManager
{
    [Inject]
    private DependencyManager.DependencyManager _dependencyManager { get; set; }
    public StateManager()
    {
        
    }

    public void PushState(ISessionContext context, IState state)
    {
        _dependencyManager.InjectDependencies(state);
        state.SetContext(context);
        
        if (context.GetCurrentState() == null)
        {
            context.SetCurrentState(state);
        }
        else
        {
            state.SetParentState(context.GetCurrentState());
            context.SetCurrentState(state);
        }
        state.Initialize();
    }

    public void PopState(ISessionContext context)
    {
        var currentState = context.GetCurrentState();
        currentState.Unload();
        var newState = currentState.GetParentState();
        context.SetCurrentState(newState);
        newState.Activate();
        
    }
}