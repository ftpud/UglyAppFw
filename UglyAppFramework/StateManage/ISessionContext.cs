namespace UglyAppFramework.StateManage;

public interface ISessionContext
{
    public IState GetCurrentState();
    public void SetCurrentState(IState state);
}