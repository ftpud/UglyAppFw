using TgUI.States;

namespace TgUI.View;

public interface IView
{
    public ViewResponse Display(State viewModel);
}