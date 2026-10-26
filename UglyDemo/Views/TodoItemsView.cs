using UglyFwUI.Rendering;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyDemo.Models;
using UglyDemo.ViewModels;
using UglyTgApplication.States;
using UglyTgApplication.View;

namespace UglyDemo.Views;

[Managed]
public class TodoItemsView : IView
{
    public ViewResponse Display(IState genericViewModel)
    {
        var viewModel = (TodoItemsViewModel)genericViewModel;

        return new ViewSimpleResponse("Here are some todo items for you:\n" +
                                      $"{String.Join("\n", viewModel.MyItems.Where(i => i != null).Select(i => Convert(i!)))}",
            ViewHelper.ButtonBuilder.Create()
                .Add("Back", "/back")
                .Add("Add", "/add")
                .Add("Remove", "/remove", viewModel.MyItems.Count > 0)
                .Add("Mark", "/mark", viewModel.MyItems.Count > 0)
                .Build()
        );
    }

    private String Convert(TodoItem item)
    {
        return HtmlText.Encode(item.Text) + (item.IsFinished ? " [ DONE ]" : "");
    }
}