using TgUI.Entity;
using TgUI.View;

namespace TgUIForm.Elements;

public class UiTextDialogView : IView
{
    public ViewResponse Display(State viewModel)
    {
        UiTextDialog testViewModel = (UiTextDialog)viewModel;
        return new ViewSimpleResponse($@"{testViewModel.Text}
{testViewModel.ErrorMessage}");
    }
}