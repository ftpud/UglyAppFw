using Telegram.Bot.Types;
using UglyAppFramework.DependencyManager.Attributes;
using UglyDemo.Model;
using UglyDemo.UI.Elements;
using UglyDemo.View;
using UglyTgApplication.Attributes;

namespace UglyDemo.ViewModel;

[BindView(typeof(TodoItemsView))]
public class TodoItemsViewModel : UglyTgApplication.States.ViewModel
{
    [Inject] internal ItemRepository _repository { get; set; }

    internal List<TodoItem?> MyItems => _repository.GetAllItemsByUserId(Context.CurrentUserId.Identifier.Value);

    [Callback(Trigger = "/remove")]
    public void RemoveController(Update update)
    {
        Push(new UiPickerDialog("Please select item to remove.",
            GetDictionaryModel(), (s, o) =>
            {
                _repository.RemoveItem((TodoItem)o);
                UpdateView();
            }
        ));
    }

    [Callback(Trigger = "/add")]
    public void AddController(Update update)
    {
        Push(new UiTextDialog("Please enter item:", s => AddRecord(s), s => String.Empty));
    }

    [Callback(Trigger = "/mark")]
    public void MarkController(Update update)
    {
        Push(new UiPickerDialog("Please select item to mark.",
            GetDictionaryModel(), (s, o) =>
            {
                ((TodoItem)o).IsFinished = !((TodoItem)o).IsFinished;
                UpdateView();
            }
        ));
    }

    private void AddRecord(String record)
    {
        if (_repository.GetAllItemsByUserId(Context.CurrentUserId.Identifier.Value).Any(e => e.Text == record))
        {
            return;
        }

        _repository.UpsertItem(new TodoItem()
        {
            Text = record,
            IsFinished = false,
            TelegramUserId = Context.CurrentUserId.Identifier.Value
        });
        UpdateView();
    }

    private Dictionary<string, Object> GetDictionaryModel()
    {
        Dictionary<string, Object> model = new Dictionary<string, object>();
        foreach (var todoItem in MyItems)
        {
            model.Add(todoItem.Text, todoItem);
        }

        return model;
    }

    [Callback(Trigger = "/back")]
    public void ControllerBack(Update update)
    {
        Pop();
    }
}