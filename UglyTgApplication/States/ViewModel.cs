using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Telegram.Bot.Types;
using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication.Attributes;

namespace UglyTgApplication.States;

public class ViewModel : TgState
{
    private readonly Dictionary<string, Func<Update, CancellationToken, Task>> _callbacks = new();
    [Inject] internal TelegramInteractionManager _telegramInteractionManager { get; set; } = null!;

    public ViewModel()
    {
        foreach (var method in GetType().GetMethods())
        {
            var attribute = method.GetCustomAttribute<CallbackAttribute>(true);
            if (attribute == null) continue;
            var parameters = method.GetParameters();
            var validParameters = parameters.Length is 1 or 2 && parameters[0].ParameterType == typeof(Update)
                && (parameters.Length == 1 || parameters[1].ParameterType == typeof(CancellationToken));
            if (!validParameters || (method.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(method.ReturnType))
                || method.IsGenericMethodDefinition
                || (method.ReturnType == typeof(void) && method.IsDefined(typeof(AsyncStateMachineAttribute))))
                throw new InvalidOperationException($"Callback {method.Name} must return void or Task and accept Update and optionally CancellationToken. async void is unsupported.");

            RegisterCallback(attribute.Trigger, async (update, token) =>
            {
                object? result;
                try
                {
                    result = method.Invoke(this, parameters.Length == 1
                        ? new object[] { update } : new object[] { update, token });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw;
                }
                if (result is Task task) await task;
            });
        }
    }

    public void RegisterCallback(string trigger, Func<Update, CancellationToken, Task> callback) =>
        _callbacks.Add(trigger, callback);

    public void RegisterCallback(string trigger, Action<Update> callback)
    {
        if (callback.Method.IsDefined(typeof(AsyncStateMachineAttribute)))
            throw new ArgumentException("Use a Task-returning callback instead of async void.", nameof(callback));
        RegisterCallback(trigger, (update, _) => { callback(update); return Task.CompletedTask; });
    }

    public sealed override async Task UpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trigger = update.CallbackQuery?.Data ?? update.Message?.Text;
        var top = Context.CurrentState;
        if (trigger != null && _callbacks.TryGetValue(trigger, out var callback))
            await callback(update, cancellationToken);
        // A callback may navigate; then the update is not delivered to the message hook as well.
        if (ReferenceEquals(Context.CurrentState, top))
            await OnMessageAsync(update, cancellationToken);
    }

    public virtual Task OnMessageAsync(Update update, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UpdateViewAsync(CancellationToken cancellationToken = default) =>
        _telegramInteractionManager.DisplayViewAsync(Context, this, cancellationToken);
    public override Task InitializeAsync(CancellationToken cancellationToken = default) => UpdateViewAsync(cancellationToken);
    public override Task ActivateAsync(CancellationToken cancellationToken = default) => UpdateViewAsync(cancellationToken);

    public override Task UnloadAsync(CancellationToken cancellationToken = default) => RemoveViewAsync(cancellationToken);

    // A covered page is hidden; ActivateAsync sends it again (with its current state) when uncovered.
    public override Task DeactivateAsync(CancellationToken cancellationToken = default) => RemoveViewAsync(cancellationToken);

    private async Task RemoveViewAsync(CancellationToken cancellationToken)
    {
        while (Messages.Count > 0)
        {
            await _telegramInteractionManager.RemoveMessageAsync(Context, Messages[0].Id, cancellationToken);
            Messages.RemoveAt(0);
        }
    }

    // Rendering is explicit: setters cannot await network requests.
    protected bool SetProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        return true;
    }
}
