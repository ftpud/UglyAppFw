using Telegram.Bot.Types;
using TgUI_Demo.Model;
using TgUI_Demo.View;
using TgUI.Attributes;
using TgUIForm.Elements;

namespace TgUI_Demo.ViewModel;

[BindView(typeof(TodoItemsView))]
public class TodoItemsViewModel : TgUI.Entity.ViewModel
{
    [Inject]
    internal ItemRepository _repository { get; set; }

    internal List<TodoItem?> MyItems => _repository.GetAllItemsByUserId(Context.CurrentUserId.Identifier.Value);

    [Callback(Trigger = "/remove")]
    public void RemoveController(Update update)
    {
        Push(new UiPickerDialog("Please select item to remove.",
            GetDictionaryModel(), (s, o) =>
            {
                _repository.RemoveItem((TodoItem)o);
                PropertyChanged();
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
                PropertyChanged();
            }
        ));
    }

    private void AddRecord(String record)
    {
        _repository.UpsertItem(new TodoItem()
        {
            Text = record,
            IsFinished = false,
            TelegramUserId = Context.CurrentUserId.Identifier.Value
        });
        PropertyChanged();
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

}