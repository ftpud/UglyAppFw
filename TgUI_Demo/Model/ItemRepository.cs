using TgUI.Attributes;
using TgUI.DependencyManager.Attributes;

namespace TgUI_Demo.Model;

// In-memory repository
[Managed]
public class ItemRepository : IRepository
{
    private static int _id;
    
    private Dictionary<long, Dictionary<int, TodoItem?>> _itemRepository =
        new Dictionary<long, Dictionary<int, TodoItem?>>();

    public List<TodoItem?> GetAllItemsByUserId(long uid)
    {
        if (!_itemRepository.ContainsKey(uid))
        {
            _itemRepository.Add(uid, new Dictionary<int, TodoItem?>());
        }

        return _itemRepository[uid].Values.ToList();
        
    }

    public TodoItem? GetItemById(long tgUid, int id)
    {
        var userRepo = _itemRepository[tgUid];
        if (userRepo.ContainsKey(id))
        {
            return userRepo[id];
        }
        else
        {
            return null;
        }
    }

    public void UpsertItem(TodoItem item)
    {
        var uid = item.TelegramUserId;
        if (item.Id == null)
        {
            item.Id = ++_id;
        }
        int id = item.Id.Value;

        if (_itemRepository.ContainsKey(uid))
        {
            var userRepo = _itemRepository[uid];
            if (userRepo.ContainsKey(id))
            {
                userRepo[id] = item;
            }
            else
            {
                userRepo.Add(id, item);
            }
        }
        else
        {
            _itemRepository.Add(uid, new Dictionary<int, TodoItem?>()
            {
                { id, item }
            });
        }
    }

    public void RemoveItem(TodoItem item)
    {
        var uid = item.TelegramUserId;
        var id = item.Id;
        
        if (_itemRepository.ContainsKey(uid))
        {
            if (_itemRepository[uid].ContainsKey(id.Value))
            {
                _itemRepository[uid].Remove(id.Value);
            }
        }
    }
}