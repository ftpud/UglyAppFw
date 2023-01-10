using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgUI.View;

public class ViewResponse
{
    public ResponseData[] ResponseMessages { get; set; }

    public virtual ResponseData[] GetResponse()
    {
        return ResponseMessages;
    }
}