using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TgUI.States;

namespace TgUI;

public class TgApplication
{
    private Type _startingState;
    private TelegramBotClient _telegramBotClient;
    private SessionManager _sessionManager;
    private StateManager _stateManager;
    
    private DependencyManager.DependencyManager _dependencyManager;
    
    public TgApplication(String token, Type startingState)
    {
        _telegramBotClient = new TelegramBotClient(token);
        _startingState = startingState;
        _sessionManager = new SessionManager();
        _stateManager = new StateManager(_telegramBotClient);
        _dependencyManager = new DependencyManager.DependencyManager();
        _dependencyManager.InjectDependencies(_stateManager);
    }

    public void Start()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates =
                { }
        };
        _telegramBotClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        long userId;
        if (update.CallbackQuery != null)
        {
            userId = update.CallbackQuery.From.Id;
        }
        else if (update.Message != null)
        {
            userId = update.Message.From.Id;
        }
        else
        {
            // ignore
            return;
        }

        var context = _sessionManager.getCurrentContext(userId);
        if (context.CurrentState == null)
        {
            // no current state
            context.CurrentUserId = userId;
            _stateManager.PushState(context, (State)Activator.CreateInstance(_startingState));
        }

        context.CurrentState.Update(update);
        
        
        if (context.CurrentState.RemoveUserMessagesPolicyEnabled && update.Message != null)
        {
            _telegramBotClient.DeleteMessageAsync(userId, update.Message.MessageId);
        }
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
    }
}