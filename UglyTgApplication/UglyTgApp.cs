using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using UglyAppFramework;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.Interfaces;
using UglyAppFramework.StateManage;

namespace UglyTgApplication;

public class UglyTgApp : IUglyLoader
{
    public static void Start(String token, Type startingState) { UglyApp.Start(new UglyTgApp(token, startingState)); }
    
    private Type _startingState;
    [Inject] private SessionManager SessionManager { get; set; }
    [Inject] private StateManager StateManager { get; set; }

    [Inject] private TelegramInteractionManager TelegramInteractionManager { get; set; }

    private string _token;
    
    public UglyTgApp(String token, Type startingState)
    {
        _token = token;
        _startingState = startingState;
    }

    public void Load()
    {
        TelegramInteractionManager.Load(_token);
        
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates =
                { }
        };
        TelegramInteractionManager._telegramBotClient.StartReceiving(
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

        var context = SessionManager.getCurrentContext(userId);
        if (context.CurrentState == null)
        {
            // no current state
            context.CurrentUserId = userId;
            StateManager.PushState(context, (State)Activator.CreateInstance(_startingState));
            
        }

        context.CurrentState.Update(update);
        
        
        if (context.CurrentState.RemoveUserMessagesPolicyEnabled && update.Message != null)
        {
            TelegramInteractionManager._telegramBotClient.DeleteMessageAsync(userId, update.Message.MessageId);
        }
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
    }
}