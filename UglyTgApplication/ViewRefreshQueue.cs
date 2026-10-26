using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication.States;

namespace UglyTgApplication;

// Re-renders views of other sessions (e.g. a shared chat room) without deadlocking:
// handlers only enqueue; the pump takes each target session's lock in turn.
[Managed]
public class ViewRefreshQueue
{
    private readonly object _sync = new();
    private readonly List<ViewModel> _pending = new();
    private readonly SemaphoreSlim _signal = new(0);

    // Set by UglyTgApp.Load while polling; offline callers drain with ProcessPendingAsync.
    public Task? Completion { get; private set; }

    public int PendingCount
    {
        get { lock (_sync) return _pending.Count; }
    }

    // Repeated requests for the same view coalesce into a single render.
    public void Request(ViewModel state)
    {
        lock (_sync)
        {
            if (_pending.Any(pending => ReferenceEquals(pending, state))) return;
            _pending.Add(state);
        }
        _signal.Release();
    }

    public void Start(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (Completion != null) throw new InvalidOperationException("The refresh queue is already running.");
            Completion = RunAsync(cancellationToken);
        }
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        while (TryTake(out var state))
            await RefreshAsync(state, cancellationToken);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(cancellationToken);
                if (TryTake(out var state)) await RefreshAsync(state, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // One stale or failing view must not stop updates for everyone else.
                Console.Error.WriteLine($"View refresh failed ({ex.GetType().Name}): {ex.Message}");
            }
        }
    }

    private bool TryTake(out ViewModel state)
    {
        lock (_sync)
        {
            if (_pending.Count == 0)
            {
                state = null!;
                return false;
            }
            state = _pending[0];
            _pending.RemoveAt(0);
            return true;
        }
    }

    private static async Task RefreshAsync(ViewModel state, CancellationToken cancellationToken)
    {
        var context = state.Context;
        await context.UpdateLock.WaitAsync(cancellationToken);
        try
        {
            // Only the visible page is rendered; covered pages render when reactivated.
            if (ReferenceEquals(context.CurrentState, state))
                await state.UpdateViewAsync(cancellationToken);
        }
        finally { context.UpdateLock.Release(); }
    }
}
