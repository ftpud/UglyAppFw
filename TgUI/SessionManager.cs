using TgUI.Entity;


namespace TgUI;

public class SessionManager
{
    private Dictionary<long, SessionContext> _sessionRepository = new Dictionary<long, SessionContext>();
    
    public SessionContext getCurrentContext(long telegramUserId)
    {
        if (!_sessionRepository.ContainsKey(telegramUserId))
        {
            _sessionRepository.Add(telegramUserId, new SessionContext());
        }

        return _sessionRepository[telegramUserId];
    }
}