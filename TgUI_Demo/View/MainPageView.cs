using TgUI.Entity;
using TgUI.View;

namespace TgUI_Demo.View;

public class MainPageView : IView
{
    public ViewResponse Display(State viewModel)
    {
        return new ViewSimpleResponse("Welcome to simple ToDo app example. /start");
    }
}