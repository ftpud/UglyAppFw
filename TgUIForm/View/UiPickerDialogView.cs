using Telegram.Bot.Types.ReplyMarkups;
using TgUI.Entity;
using TgUI.View;

namespace TgUIForm.Elements;

public class UiPickerDialogView : IView
{
    public ViewResponse Display(State viewModel)
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