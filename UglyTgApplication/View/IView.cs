using UglyAppFramework.StateManage;
using UglyTgApplication.States;

namespace UglyTgApplication.View;

public interface IView
{
    public ViewResponse Display(IState viewModel);
}