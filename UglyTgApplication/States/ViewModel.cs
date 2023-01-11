using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication.Attributes;
using UglyTgApplication.Entity;

namespace UglyTgApplication.States;

public class ViewModel : TgState
{
    private Dictionary<String, Action<Update>> _callbackRepository = new Dictionary<string, Action<Update>>();

    [Inject]
    internal TelegramInteractionManager _telegramInteractionManager { get; set; }
    
    public ViewModel()
    {
        MethodInfo[] methods = this.GetType().GetMethods();
        foreach (var methodInfo in methods)
        {
            object[] attributesArray = methodInfo.GetCustomAttributes(true);
            foreach (var attribute in attributesArray)
            {
                if (attribute is CallbackAttribute)
                {
                    _callbackRepository.Add(((CallbackAttribute)attribute).Trigger,
                        update => methodInfo.Invoke(this, new[] { update }));
                }
            }
        }
    }

    protected void RegisterCallback(String trigger, Action<Update> callback)
    {
        _callbackRepository.Add(trigger, callback);
    }

    public sealed override void Update(Update update)
    {
        if (update.Message != null && _callbackRepository.ContainsKey(update.Message.Text))
        {
            _callbackRepository[update.Message.Text].Invoke(update);
        }
        else if (update.Type == UpdateType.CallbackQuery && _callbackRepository.ContainsKey(update.CallbackQuery.Data))
        {
            _callbackRepository[update.CallbackQuery.Data].Invoke(update);
        }

        OnMessage(update);
    }

    public virtual void OnMessage(Update update)
    {
    }

    protected void PropertyChanged()
    {
        _telegramInteractionManager.DisplayView((SessionContext)GetContext(), this);
    }

    public override void Initialize()
    {
        _telegramInteractionManager.DisplayView((SessionContext)GetContext(), this);
    }

    public override void Unload()
    {
        for (int i = 0; i < Context.CurrentState.MessageId.Length; i++)
        {
            _telegramInteractionManager.RemoveMessage(Context, Context.CurrentState.MessageId[i]);
        }
    }

    protected bool SetProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged();
        return true;
    }
}