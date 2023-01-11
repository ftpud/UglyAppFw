namespace UglyDemo.Model;

public class TodoItem
{
    public int? Id { get; set; }
    public long TelegramUserId { get; set; }
    public String Text { get; set; }
    public bool IsFinished { get; set; }
}