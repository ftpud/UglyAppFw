namespace UglyAppFramework.StateManage;

public interface ISessionContext
{
    IState? GetCurrentState();
    void SetCurrentState(IState? state);
}
