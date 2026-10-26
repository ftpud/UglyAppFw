using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.StateManage;

[Managed]
public class StateManager
{
    [Inject] private DependencyManager.DependencyManager _dependencyManager { get; set; } = null!;

    public async Task PushStateAsync(ISessionContext context, IState state,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parent = context.GetCurrentState();
        for (var ancestor = parent; ancestor != null; ancestor = ancestor.GetParentState())
            if (ReferenceEquals(ancestor, state))
                throw new InvalidOperationException("A state already on the navigation stack cannot be pushed again.");
        _dependencyManager.InjectDependencies(state);
        state.SetContext(context);
        state.SetParentState(parent);
        context.SetCurrentState(state);
        try
        {
            await state.InitializeAsync(cancellationToken);
        }
        catch
        {
            context.SetCurrentState(parent);
            // Clean up messages sent before initialization failed, preserving the original error.
            try { await state.UnloadAsync(CancellationToken.None); }
            catch { /* The original initialization error is propagated to the caller. */ }
            throw;
        }
    }

    public async Task PopStateAsync(ISessionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = context.GetCurrentState();
        var parent = current?.GetParentState();
        if (current == null || parent == null) return;
        await current.UnloadAsync(cancellationToken);
        context.SetCurrentState(parent);
        await parent.ActivateAsync(cancellationToken);
    }
}
