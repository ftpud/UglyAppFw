using UglyDemo.Services;
using Telegram.Bot.Types;
using UglyAppFramework.DependencyManager.Attributes;
using UglyDemo.Models;
using UglyDemo.Views;
using UglyFwUI.Models;
using UglyFwUI.ViewModels;
using UglyTgApplication.Attributes;

namespace UglyDemo.ViewModels;

[BindView(typeof(TodoItemsView))]
public class TodoItemsViewModel : UglyTgApplication.States.ViewModel
{
    [Inject] internal ItemRepository _repository { get; set; } = null!;
    internal List<TodoItem?> MyItems => _repository.GetAllItemsByUserId(Context.UserId);

    [Callback(Trigger = "/remove")]
    public Task RemoveController(Update update, CancellationToken cancellationToken) =>
        PushAsync(new PickerDialogViewModel<TodoItem>("Please select item to remove.", GetOptions(), (option, _) =>
        {
            _repository.RemoveItem(option.Value);
            return Task.CompletedTask;
        }), cancellationToken);

    [Callback(Trigger = "/add")]
    public Task AddController(Update update, CancellationToken cancellationToken) =>
        PushAsync(new TextDialogViewModel("Please enter item:", AddRecord,
            text => string.IsNullOrWhiteSpace(text) ? "Please enter a non-empty item." : null), cancellationToken);

    [Callback(Trigger = "/mark")]
    public Task MarkController(Update update, CancellationToken cancellationToken) =>
        PushAsync(new PickerDialogViewModel<TodoItem>("Please select item to mark.", GetOptions(), (option, _) =>
        {
            option.Value.IsFinished = !option.Value.IsFinished;
            return Task.CompletedTask;
        }), cancellationToken);

    private Task AddRecord(string record, CancellationToken cancellationToken)
    {
        if (!_repository.GetAllItemsByUserId(Context.UserId).Any(e => e?.Text == record))
            _repository.UpsertItem(new TodoItem { Text = record, IsFinished = false, TelegramUserId = Context.UserId });
        return Task.CompletedTask;
    }

    private IEnumerable<PickerOption<TodoItem>> GetOptions() => MyItems
        .Where(item => item != null).Select(item => new PickerOption<TodoItem>(item!.Text, item));

    [Callback(Trigger = "/back")]
    public Task ControllerBack(Update update, CancellationToken cancellationToken) => PopAsync(cancellationToken);
}
