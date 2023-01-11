using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyDemo.Model;
using UglyDemo.ViewModel;
using UglyTgApplication.States;
using UglyTgApplication.View;

namespace UglyDemo.View;

[Managed]
public class TodoItemsView : IView
{
    public ViewResponse Display(IState genericViewModel)
    {
        var viewModel = (TodoItemsViewModel)genericViewModel;

        return new ViewSimpleResponse("Here are some todo items for you:\n" +
                                      $"{String.Join("\n", viewModel.MyItems.Select(i => Convert(i)))}",
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
        return item.Text + (item.IsFinished ? " [ DONE ]" : "");
    }
}