using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyDemo.UI.Elements;
using UglyTgApplication.View;

namespace UglyDemo.UI.View;

[Managed]
public class UiTextDialogView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        UiTextDialog testViewModel = (UiTextDialog)viewModel;
        return new ViewSimpleResponse($@"{testViewModel.Text}
{testViewModel.ErrorMessage}");
    }
}