using Telegram.Bot.Types;
using TgUI_Demo.View;
using TgUI.Attributes;


namespace TgUI_Demo.ViewModel;

[BindView(view: typeof(MainPageView))]
public class MainPageViewModel : TgUI.States.ViewModel
{
    [Callback(Trigger = "/start")]
    public void Controller(Update update)
    {
        Push(new TodoItemsViewModel());
    }
}