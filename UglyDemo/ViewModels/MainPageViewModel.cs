using Telegram.Bot.Types;
using UglyDemo.Views;
using UglyTgApplication.Attributes;

namespace UglyDemo.ViewModels;

[BindView(typeof(MainPageView))]
public class MainPageViewModel : UglyTgApplication.States.ViewModel
{
    [Callback(Trigger = "/start")]
    public Task Controller(Update update, CancellationToken cancellationToken) =>
        PushAsync(new TodoItemsViewModel(), cancellationToken);

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine(Context.UserId + " connected");
        return base.InitializeAsync(cancellationToken);
    }
}
