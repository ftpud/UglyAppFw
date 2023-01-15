using UglyAppFramework.StateManage;

namespace UglyTgApplication.View;

public interface IView
{
    public ViewResponse Display(IState viewModel);
}