using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UglyTgApplication.View;

public class ResponseData
{
    // Optional stable identity for a message in a fully keyed page.
    public string? Key { get; set; }
    // Optional encoded photo. text becomes its HTML caption.
    public byte[]? Photo { get; set; }
    public String text { get; set; }
    public ParseMode parseMode { get; set; } = ParseMode.Html;
    public  InlineKeyboardMarkup? replyMarkup { get; set; } = null;
    // Photo messages only: show the caption above the image (Bot API show_caption_above_media).
    public bool CaptionAboveMedia { get; set; }
}