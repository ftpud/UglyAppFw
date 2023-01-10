using Telegram.Bot.Types.ReplyMarkups;

namespace TgUI.View;

public class ViewSimpleResponse : ViewResponse
{
  public ViewSimpleResponse(String text, InlineKeyboardMarkup markup = null)
  {
    ResponseMessages = new[]
    {
      new ResponseData()
      {
        text = text, 
        replyMarkup = markup
      }
    };
  }

  public override ResponseData[] GetResponse()
  {
    return base.GetResponse();
  }
}