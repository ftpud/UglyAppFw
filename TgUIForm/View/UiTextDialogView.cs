using TgUI.States;
using TgUI.View;
using TgUIForm.Elements;

namespace TgUIForm.View;

public class UiTextDialogView : IView
{
    public ViewResponse Display(State viewModel)
    {
        UiTextDialog testViewModel = (UiTextDialog)viewModel;
        return new ViewSimpleResponse($@"{testViewModel.Text}
{testViewModel.ErrorMessage}");
    }
}