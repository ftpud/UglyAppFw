using System.Reflection;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgUI.Attributes;

namespace TgUI.Entity;

public class ViewModel : State
{
    private Dictionary<String, Action<Update>> _callbackRepository = new Dictionary<string, Action<Update>>();

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
            //_callbackRepository[update.Message.Text].Invoke(this, new[] { update });
            _callbackRepository[update.Message.Text].Invoke(update);
        }
        else if (update.Type == UpdateType.CallbackQuery && _callbackRepository.ContainsKey(update.CallbackQuery.Data))
        {
            //_callbackRepository[update.CallbackQuery.Data].Invoke(this, new[] { update });
            _callbackRepository[update.CallbackQuery.Data].Invoke(update);
        }

        OnMessage(update);
    }

    public virtual void OnMessage(Update update)
    {
    }

    protected void PropertyChanged()
    {
        _stateManager.DisplayView(Context, this);
    }

    protected bool SetProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged();
        return true;
    }
}