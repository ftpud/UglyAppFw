using System.Resources;
using Telegram.Bot.Types;
using TgUI_Demo.View;
using TgUI.Attributes;

namespace TgUI_Demo.ViewModel;

[TgUI.Attributes.View(typeof(Page2View))]
public class Page2ViewModel : TgUI.Entity.ViewModel
{
    internal String data = "qeq";

    [CallBack(Trigger = "/change")]
    public void Change(Update update)
    {
        data += " 👀";
        PropertyChanged();
    }
    
    [CallBack(Trigger = "/back")]
    public void Back(Update update)
    {
        Pop();
    }
}