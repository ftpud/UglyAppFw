using TgUI.Entity;

namespace TgUI.View;

public interface IView
{
    public ViewResponse Display(State viewModel);
}