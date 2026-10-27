using Telegram.Bot.Types;
using UglyAppFramework.StateManage;
using UglyTgApplication.Entity;

namespace UglyTgApplication.States;

public class TgState : State
{
    public bool RemoveUserMessagesPolicyEnabled { get; set; } = true;
    internal List<(int Id, string Fingerprint, bool IsPhoto, string? Key, string? PhotoHash)> Messages { get; } = new();
    private ISessionContext _sessionContext = new SessionContext();
    public SessionContext Context => (SessionContext)GetContext();

    public virtual Task UpdateAsync(Update update, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public sealed override ISessionContext GetContext() => _sessionContext;
    public override void SetContext(ISessionContext context) => _sessionContext = context;
}
