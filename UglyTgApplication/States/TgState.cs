using Telegram.Bot.Types;
using UglyAppFramework.StateManage;
using UglyTgApplication.Entity;

namespace UglyTgApplication.States;

public class TgState : State
{
    public bool RemoveUserMessagesPolicyEnabled { get; set; } = true;

    internal int[] MessageId { get; set; }
    
    private ISessionContext _sessionContext = new SessionContext();

    public SessionContext Context => (SessionContext)GetContext();
    
    public override void Initialize()
    {
        
    }

    public virtual void Update(Update update)
    {
        
    }

    public sealed override ISessionContext GetContext()
    {
        return _sessionContext;
    }

    public override void SetContext(ISessionContext context)
    {
        _sessionContext = context;
    }
    
}