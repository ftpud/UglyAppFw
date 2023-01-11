using UglyTgApplication.States;

namespace UglyTgApplication.Entity;

public class Session
{
    public String Id { get; set; }
    public TgState CurrentState { get; set; }
}