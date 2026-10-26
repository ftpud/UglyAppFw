using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UglyAppFramework;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.Interfaces;
using UglyAppFramework.StateManage;
using UglyTgApplication.States;
using UglyTgApplication.Services;

namespace UglyTgApplication;

public class UglyTgApp : IUglyLoader
{
    // Null honors each state's policy; an application can enforce one policy for all pages/dialogs.
    public bool? RemoveUserMessagesPolicyOverride { get; set; }

    public static void Start(string token, Type startingState, CancellationToken cancellationToken = default,
        IUiMessageStore? messageStore = null) =>
        UglyApp.Start(new UglyTgApp(token, startingState, cancellationToken, messageStore));

    private readonly Type _startingState;
    private readonly string _token;
    private readonly CancellationToken _shutdown;
    private readonly IUiMessageStore? _messageStore;
    [Inject] private SessionManager SessionManager { get; set; } = null!;
    [Inject] private StateManager StateManager { get; set; } = null!;
    [Inject] private TelegramInteractionManager TelegramInteractionManager { get; set; } = null!;
    [Inject] private ViewRefreshQueue ViewRefreshQueue { get; set; } = null!;

    public UglyTgApp(string token, Type startingState, CancellationToken cancellationToken = default,
        IUiMessageStore? messageStore = null)
    {
        if (!typeof(TgState).IsAssignableFrom(startingState) || startingState.IsAbstract
            || startingState.GetConstructor(Type.EmptyTypes) == null)
            throw new ArgumentException("Starting state must be a concrete TgState with a public parameterless constructor.", nameof(startingState));
        _token = token;
        _startingState = startingState;
        _shutdown = cancellationToken;
        _messageStore = messageStore;
    }

    public void Load()
    {
        TelegramInteractionManager.Load(_token);
        if (_messageStore != null) TelegramInteractionManager.MessageStore = _messageStore;
        TelegramInteractionManager._telegramBotClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery } }, _shutdown);
        ViewRefreshQueue.Start(_shutdown);
    }

    public virtual async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var message = update.CallbackQuery?.Message ?? update.Message;
        var sender = update.CallbackQuery?.From ?? update.Message?.From;
        if (update.CallbackQuery != null)
        {
            try
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 &&
                (ex.Message.Contains("query is too old", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("query ID is invalid", StringComparison.OrdinalIgnoreCase)))
            {
                // An expired acknowledgement must not prevent routing the button action.
            }
        }
        // Inline-mode callbacks have no chat to render into. Anonymous/channel messages have no sender.
        if (message == null || sender == null || sender.IsBot) return;

        var context = SessionManager.GetCurrentContext(message.Chat.Id, sender.Id);
        await context.UpdateLock.WaitAsync(cancellationToken);
        try
        {
            if (context.CurrentState == null)
            {
                await TelegramInteractionManager.CleanPreviousViewsAsync(context, cancellationToken);
                await StateManager.PushStateAsync(context, (TgState)Activator.CreateInstance(_startingState)!, cancellationToken);
            }
            var state = context.CurrentState!;
            await state.UpdateAsync(update, cancellationToken);
            if ((RemoveUserMessagesPolicyOverride ?? state.RemoveUserMessagesPolicyEnabled) && update.Message != null)
                await TelegramInteractionManager.RemoveMessageAsync(context, update.Message.MessageId, cancellationToken);
        }
        finally { context.UpdateLock.Release(); }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"Telegram update failed ({exception.GetType().Name}): {exception.Message}");
        return Task.CompletedTask;
    }
}
