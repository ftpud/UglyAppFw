using Telegram.Bot;
using TgUI.Attributes;
using TgUI.Entity;
using TgUI.View;

namespace TgUI;

public class StateManager
{
    private SessionManager _sessionManager;
    private TelegramBotClient _telegramBotClient { get; set; }

    [Inject] private DependencyManager _dependencyManager { get; set; }

    public StateManager(TelegramBotClient telegramBotClient)
    {
        _telegramBotClient = telegramBotClient;
    }

    internal void PushState(SessionContext context, State state)
    {
        _dependencyManager.InjectDependencies(state);
        
        state.SetContext(context);
        state._stateManager = this;

        if (context.CurrentState == null)
        {
            context.CurrentState = state;
        }
        else
        {
            state.ParentState = context.CurrentState;
            context.CurrentState = state;
        }

        context.CurrentState.Initialize();
        DisplayView(context, state);
    }


    internal void DisplayView(SessionContext context, State state)
    {
        var view = _dependencyManager.GetView(state);

        if (state.MessageId == null)
        {
            SendViewMessage(view, context, state);
        }
        else
        {
            UpdateViewMessage(view, context, state);
        }
    }

    private void SendViewMessage(IView view, SessionContext context, State state)
    {
        ViewResponse viewResponse = view.Display(state);
        state.MessageId = viewResponse.ResponseMessages.ToList().Select(r => _telegramBotClient
            .SendTextMessageAsync(
                chatId: context.CurrentUserId,
                text: r.text,
                replyMarkup: r.replyMarkup,
                parseMode: r.parseMode
            ).Result.MessageId).ToArray();
    }

    private void UpdateViewMessage(IView view, SessionContext context, State state, bool inactive = false)
    {
        ViewResponse viewResponse = view.Display(state);

        for (int i = 0; i < viewResponse.ResponseMessages.Length; i++)
        {
            var response = viewResponse.ResponseMessages[i];
            _telegramBotClient.EditMessageTextAsync(
                chatId: context.CurrentUserId,
                messageId: state.MessageId[i],
                text: response.text,
                parseMode: response.parseMode,
                replyMarkup: inactive ? null : response.replyMarkup);
        }
    }

    internal void PopState(SessionContext context)
    {
        for (int i = 0; i < context.CurrentState.MessageId.Length; i++)
        {
            _telegramBotClient.DeleteMessageAsync(context.CurrentUserId, context.CurrentState.MessageId[i]);
        }

        context.CurrentState = context.CurrentState.ParentState;
    }
    
}