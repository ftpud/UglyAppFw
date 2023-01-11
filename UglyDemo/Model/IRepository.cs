namespace UglyDemo.Model;

public interface IRepository
{
    public List<TodoItem?> GetAllItemsByUserId(long tgUid);
    public TodoItem? GetItemById(long tgUid, int id);

    public void UpsertItem(TodoItem item);
    public void RemoveItem(TodoItem item);

}