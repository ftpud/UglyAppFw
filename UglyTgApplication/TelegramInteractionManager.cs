using Telegram.Bot;
using UglyAppFramework.DependencyManager;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyTgApplication.Attributes;
using UglyTgApplication.Entity;
using UglyTgApplication.States;
using UglyTgApplication.View;

namespace UglyTgApplication;

[Managed]
public class TelegramInteractionManager
{
    public TelegramBotClient _telegramBotClient { get; set; }
    
    [Inject] private DependencyManager DependencyManager { get; set; }

    public TelegramInteractionManager()
    {
        
    }

    public void Load(String token)
    {
        _telegramBotClient = new TelegramBotClient(token);
        
    }
    
    internal void DisplayView(SessionContext context, TgState state)
    {
        var view = GetView(state);

        if (state.MessageId == null)
        {
            SendViewMessage(view, context, state);
        }
        else
        {
            UpdateViewMessage(view, context, state);
        }
    }

    internal void RemoveMessage(SessionContext Context, int msg)
    {
        _telegramBotClient.DeleteMessageAsync(Context.CurrentUserId, msg);
    }

    private void SendViewMessage(IView view, SessionContext context, TgState state)
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

    private void UpdateViewMessage(IView view, SessionContext context, TgState state, bool inactive = false)
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
    
    private IView GetView(State state)
    {
        
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(state.GetType());
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BindViewAttribute)
                {
                    BindViewAttribute attribute = (BindViewAttribute)attr;
                    var viewType = attribute.ViewType;
                    return (IView) DependencyManager.GetDependencyForType(viewType);
                }
            }

            throw new Exception("View is expected to be attached to ViewModel");
    }
}