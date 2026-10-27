using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.StateManage;

[Managed]
public class StateManager
{
    [Inject] private DependencyManager.DependencyManager _dependencyManager { get; set; } = null!;

    public Task PushStateAsync(ISessionContext context, IState state,
        CancellationToken cancellationToken = default) =>
        PushStateAsync(context, state, PushMode.Replace, cancellationToken);

    public async Task PushStateAsync(ISessionContext context, IState state, PushMode mode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parent = context.GetCurrentState();
        for (var ancestor = parent; ancestor != null; ancestor = ancestor.GetParentState())
            if (ReferenceEquals(ancestor, state))
                throw new InvalidOperationException("A state already on the navigation stack cannot be pushed again.");
        var previouslyVisible = GetVisibleStates(context);
        _dependencyManager.InjectDependencies(state);
        state.SetContext(context);
        state.SetParentState(parent);
        state.SetPushMode(mode);
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
        // The new state is shown first, then the states it replaces are hidden.
        if (mode == PushMode.Replace)
            foreach (var hidden in previouslyVisible)
                await hidden.DeactivateAsync(cancellationToken);
    }

    // Pops the top state.
    public Task PopStateAsync(ISessionContext context, CancellationToken cancellationToken = default) =>
        context.GetCurrentState() is { } current
            ? PopStateAsync(context, current, cancellationToken)
            : Task.CompletedTask;

    // Closes state together with every state above it, then (re)shows what becomes visible.
    // Popping the root, or a state that is not on the stack, is a no-op.
    public async Task PopStateAsync(ISessionContext context, IState state, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (state.GetParentState() == null || !IsOnStack(context, state)) return;
        IState current;
        do
        {
            // Unload top-down; the stack stays consistent if an unload fails, so it can be retried.
            current = context.GetCurrentState()!;
            await current.UnloadAsync(cancellationToken);
            context.SetCurrentState(current.GetParentState());
        } while (!ReferenceEquals(current, state));

        // Bottom-up, so pages that were hidden are sent again in their stacking order.
        foreach (var visible in GetVisibleStates(context).Reverse())
            await visible.ActivateAsync(cancellationToken);
    }

    // Top first: the current state, then each state below it until one pushed with Replace.
    public IReadOnlyList<IState> GetVisibleStates(ISessionContext context)
    {
        var visible = new List<IState>();
        for (var state = context.GetCurrentState(); state != null; state = state.GetParentState())
        {
            visible.Add(state);
            if (state.GetPushMode() == PushMode.Replace) break;
        }
        return visible;
    }

    // The state that receives typed input: the topmost visible state that is not a panel.
    public IState? GetFocusedState(ISessionContext context) =>
        GetVisibleStates(context).FirstOrDefault(state => state.GetPushMode() != PushMode.Panel)
        ?? context.GetCurrentState();

    // True when a modal above state blocks its input.
    public bool IsBlocked(ISessionContext context, IState state)
    {
        foreach (var visible in GetVisibleStates(context))
        {
            if (ReferenceEquals(visible, state)) return false;
            if (visible.GetPushMode() == PushMode.Modal) return true;
        }
        return false;
    }

    private static bool IsOnStack(ISessionContext context, IState state)
    {
        for (var current = context.GetCurrentState(); current != null; current = current.GetParentState())
            if (ReferenceEquals(current, state)) return true;
        return false;
    }
}
