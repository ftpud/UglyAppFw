using Telegram.Bot.Types.ReplyMarkups;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyDemo.UI.Elements;
using UglyTgApplication.View;

namespace UglyDemo.UI.View;

[Managed]
public class UiPickerDialogView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        UiPickerDialog testViewModel = (UiPickerDialog)viewModel;
        ViewResponse response = new ViewResponse();
        List<ResponseData> data = new List<ResponseData>();
        int i = 0;
        foreach (var element in testViewModel.Model)
        {
            InlineKeyboardButton selectButton = new InlineKeyboardButton("Select");
            selectButton.CallbackData = $"action_{i}";

            data.Add(new ResponseData()
                {
                    text = element.Key,
                    replyMarkup = new InlineKeyboardMarkup(
                        new[] { selectButton }
                    )
                }
            );
            i++;
        }

        response.ResponseMessages = data.ToArray();
        return response;
    }
}