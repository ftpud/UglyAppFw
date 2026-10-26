using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.StateManage;

[Managed]
public abstract class State : IState
{
    [Inject] protected StateManager _stateManager { get; set; } = null!;
    protected IState? ParentState { get; set; }

    public Task PushAsync(IState state, CancellationToken cancellationToken = default) =>
        _stateManager.PushStateAsync(GetContext(), state, cancellationToken);

    public Task PopAsync(CancellationToken cancellationToken = default) =>
        _stateManager.PopStateAsync(GetContext(), cancellationToken);

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public virtual Task UnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public virtual Task ActivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public abstract void SetContext(ISessionContext context);
    public abstract ISessionContext GetContext();
    public IState? GetParentState() => ParentState;
    public void SetParentState(IState? state) => ParentState = state;
}
