using Telegram.Bot.Types.ReplyMarkups;

namespace TgUI.View;

public static class ViewHelper
{
    public static class ButtonBuilder
    {
        public static ButtonBuilderWith Create()
        {
            return new ButtonBuilderWith();
        }
    }
    
    public class ButtonBuilderWith
    {
        private List<InlineKeyboardButton> _buttons = new List<InlineKeyboardButton>();

        public ButtonBuilderWith Add(String text, String callbackData, bool enabled=true)
        {
            if (!enabled) return this;
            
            InlineKeyboardButton button = new InlineKeyboardButton(text);
            button.CallbackData = callbackData;
            _buttons.Add(button);
            return this;
        }

        public InlineKeyboardMarkup Build()
        {
            return new InlineKeyboardMarkup(_buttons);
        }
    }
}