using Telegram.Bot.Types.ReplyMarkups;
using TgUI_Demo.Model;
using TgUI_Demo.ViewModel;
using TgUI.Entity;
using TgUI.View;

namespace TgUI_Demo.View;

public class TestView : IView
{
    public ViewResponse Display(State viewModel)
    {
        TestViewModel testViewModel = (TestViewModel)viewModel;

        InlineKeyboardButton incButton = new InlineKeyboardButton("Increase");
        incButton.CallbackData = $"/sharedInc";

        InlineKeyboardButton decButton = new InlineKeyboardButton("Decrease");
        decButton.CallbackData = $"/sharedDec";


        return new ViewSimpleResponse(
@$"Hello world.
UserId: {testViewModel.ctx.CurrentUserId}
Private value {testViewModel.someValue} /decrease /increase
Shared value {TestModel.SharedValue}
Page2 test /page2
Enter number: {testViewModel.number} /number /picker
Log:
<code>{String.Join("\n", TestModel.Online)}
</code>
", new InlineKeyboardMarkup(new[]
            {
                incButton, decButton
            })
        );
    }
}