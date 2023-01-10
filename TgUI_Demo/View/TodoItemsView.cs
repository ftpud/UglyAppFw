using TgUI_Demo.Model;
using TgUI_Demo.ViewModel;
using TgUI.Entity;
using TgUI.View;

namespace TgUI_Demo.View;

public class TodoItemsView : IView
{
    public ViewResponse Display(State genericViewModel)
    {
        var viewModel = (TodoItemsViewModel)genericViewModel;

        return new ViewSimpleResponse("Here are some todo items for you:\n" +
                                      $"{String.Join("\n", viewModel.MyItems.Select(i => Convert(i)))}",
            ViewHelper.ButtonBuilder.Create()
                .Add("Add", "/add")
                .Add("Remove", "/remove", viewModel.MyItems.Count > 0)
                .Add("Mark", "/mark", viewModel.MyItems.Count > 0)
                .Build()
        );
    }

    private String Convert(TodoItem item)
    {
        return item.Text + (item.IsFinished ? " [ DONE ]" : "");
    }
}