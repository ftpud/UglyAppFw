namespace UglyAppFramework.StateManage;

public interface IState
{
    Task PushAsync(IState state, CancellationToken cancellationToken = default);
    Task PopAsync(CancellationToken cancellationToken = default);
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task UnloadAsync(CancellationToken cancellationToken = default);
    Task ActivateAsync(CancellationToken cancellationToken = default);
    void SetContext(ISessionContext context);
    ISessionContext GetContext();
    IState? GetParentState();
    void SetParentState(IState? state);
}
