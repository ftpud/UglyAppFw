using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgUI.View;

public class ResponseData
{
    public String text { get; set; }
    public ParseMode parseMode { get; set; } = ParseMode.Html;
    public  InlineKeyboardMarkup? replyMarkup { get; set; } = null;
}