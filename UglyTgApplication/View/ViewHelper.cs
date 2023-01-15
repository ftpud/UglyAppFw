using Telegram.Bot.Types.ReplyMarkups;

namespace UglyTgApplication.View;

public static class ViewHelper
{
    public static class ButtonBuilder
    {
        public static ButtonBuilderWith Create()
        {
            return new ButtonBuilderWith();
        }

        public static InlineKeyboardMarkup BuildBackButton(String text = "Back")
        {
            return Create().Add(text, "/back").Build();
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

        public ButtonBuilderWith AddBackButton(String text = "Back")
        {
            return Add(text, "/back");
        }

        public InlineKeyboardMarkup Build()
        {
            return new InlineKeyboardMarkup(_buttons);
        }
    }
}