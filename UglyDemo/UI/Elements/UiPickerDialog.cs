using UglyDemo.UI.View;
using UglyTgApplication.Attributes;

namespace UglyDemo.UI.Elements;

[BindView(typeof(UiPickerDialogView))]
public class UiPickerDialog : UglyTgApplication.States.ViewModel
{
    internal String Text;
    internal Dictionary<String, Object> Model;
    private Action<String, Object> _callback;
    
    
    public UiPickerDialog(String text, Dictionary<String, Object> model, Action<String, Object> callback)
    {
        Model = model;
        Text = text;
        _callback = callback;
    }

    public override void Initialize()
    {
        int i = 0;
        foreach (var pair in Model)
        {
            RegisterCallback("action_" + i, update =>
            {
                Pop();
                _callback.Invoke(pair.Key, pair.Value);
            });
            i++;
        }
        
        base.Initialize();
    }
}