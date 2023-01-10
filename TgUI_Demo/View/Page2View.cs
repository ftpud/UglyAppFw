using TgUI_Demo.Model;
using TgUI_Demo.ViewModel;
using TgUI.Entity;
using TgUI.View;

namespace TgUI_Demo.View;

public class Page2View : IView
{
    public ViewResponse Display(State viewModel)
    {
        Page2ViewModel testViewModel = (Page2ViewModel)viewModel;
        return new ViewResponse()
        {
            ResponseMessages = new[]
            {
                new ResponseData()
                {
                    text = @$"Это страница 2. {testViewModel.data}",
                    replyMarkup = ViewHelper.ButtonBuilder
                        .Create()
                        .Add("o_O", "/change")
                        .Build()
                },
                new ResponseData()
                {
                    text = @$"👀 /change
Назад: /back",
                    replyMarkup = ViewHelper.ButtonBuilder
                        .Create()
                        .Add("back", "/back")
                        .Build()
                }
            }
        };
    }
}