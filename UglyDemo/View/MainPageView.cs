using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyTgApplication.States;
using UglyTgApplication.View;

namespace UglyDemo.View;

[Managed]
public class MainPageView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        return new ViewSimpleResponse("Welcome to simple ToDo app example. /start");
    }
}