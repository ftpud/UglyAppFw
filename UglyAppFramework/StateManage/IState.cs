namespace UglyAppFramework.StateManage;

public interface IState
{
    Task PushAsync(IState state, CancellationToken cancellationToken = default);
    Task PushAsync(IState state, PushMode mode, CancellationToken cancellationToken = default);
    Task PopAsync(CancellationToken cancellationToken = default);
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task UnloadAsync(CancellationToken cancellationToken = default);
    Task ActivateAsync(CancellationToken cancellationToken = default);
    // Called when a state pushed with PushMode.Replace hides this one; ActivateAsync follows when it is uncovered.
    Task DeactivateAsync(CancellationToken cancellationToken = default);
    void SetContext(ISessionContext context);
    ISessionContext GetContext();
    IState? GetParentState();
    void SetParentState(IState? state);
    PushMode GetPushMode();
    void SetPushMode(PushMode mode);
}
