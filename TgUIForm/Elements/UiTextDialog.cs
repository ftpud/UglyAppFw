using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgUI.Attributes;
using TgUI.Entity;

namespace TgUIForm.Elements;

[BindView(typeof(UiTextDialogView))]
public class UiTextDialog : ViewModel
{
    internal String Text;
    
    public String ErrorMessage {
        get { return _errorMessage; }
        set { SetProperty(ref _errorMessage, value); }
    }
    
    private String _errorMessage = "";
    
    private Action<String> _callback;
    private Func<String, String> _validator;
    
    public UiTextDialog(String text, Action<String> callback, Func<String, String> validator)
    {
        Text = text;
        _callback = callback;
        _validator = validator;
    }

    public override void OnMessage(Update update)
    {
        if (update.Type == UpdateType.Message)
        {
            var err = _validator.Invoke(update.Message.Text);
            if (err == null || err == "")
            {
                
                _callback(update.Message.Text);
                Pop();
            }
            else
            {
                ErrorMessage = err;
            }
        }
    }
}