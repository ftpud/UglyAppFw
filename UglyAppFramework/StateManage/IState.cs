namespace UglyAppFramework.StateManage;

public interface IState
{
    public void Push(IState state);
    
    public void Pop();

    public void Initialize();

    public void Unload();
    
    public void Activate();

    public void SetContext(ISessionContext context);
    
    public ISessionContext GetContext();

    public IState GetParentState();
    public void SetParentState(IState state);
}