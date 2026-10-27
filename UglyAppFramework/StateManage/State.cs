using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.StateManage;

public abstract class State : IState
{
    [Inject] protected StateManager _stateManager { get; set; } = null!;
    protected IState? ParentState { get; set; }
    protected PushMode PushMode { get; set; } = PushMode.Replace;

    // States created inside a dependency scope (e.g. an update handler) are injected on
    // construction, so derived constructors can already use their [Inject] properties.
    protected State() => DependencyManager.DependencyManager.Current?.InjectDependencies(this);

    public Task PushAsync(IState state, CancellationToken cancellationToken = default) =>
        _stateManager.PushStateAsync(GetContext(), state, cancellationToken);

    public Task PushAsync(IState state, PushMode mode, CancellationToken cancellationToken = default) =>
        _stateManager.PushStateAsync(GetContext(), state, mode, cancellationToken);

    // Closes this state (and any states pushed above it), not merely the top state.
    public Task PopAsync(CancellationToken cancellationToken = default) =>
        _stateManager.PopStateAsync(GetContext(), this, cancellationToken);

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public virtual Task UnloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public virtual Task ActivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public virtual Task DeactivateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public abstract void SetContext(ISessionContext context);
    public abstract ISessionContext GetContext();
    public IState? GetParentState() => ParentState;
    public void SetParentState(IState? state) => ParentState = state;
    public PushMode GetPushMode() => PushMode;
    public void SetPushMode(PushMode mode) => PushMode = mode;
}
