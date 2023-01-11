using Telegram.Bot.Types;
using UglyDemo.View;
using UglyTgApplication.Attributes;

namespace UglyDemo.ViewModel;

[BindView(view: typeof(MainPageView))]
public class MainPageViewModel : UglyTgApplication.States.ViewModel
{
    [Callback(Trigger = "/start")]
    public void Controller(Update update)
    {
        Push(new TodoItemsViewModel());
    }

    public override void Initialize()
    {
        Console.WriteLine(Context.CurrentUserId.Identifier + " connected");
        base.Initialize();
    }
}