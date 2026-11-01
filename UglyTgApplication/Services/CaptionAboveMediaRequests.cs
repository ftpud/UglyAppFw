using Newtonsoft.Json;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace UglyTgApplication.Services;

// Telegram.Bot 19 predates Bot API 7.3's show_caption_above_media. These request types add the
// field; it is omitted from the payload when null, so ordinary photos are sent exactly as before.

internal sealed class SendPhotoCaptionAboveRequest : SendPhotoRequest
{
    public SendPhotoCaptionAboveRequest(ChatId chatId, IInputFile photo) : base(chatId, photo) { }
    [JsonProperty("show_caption_above_media", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowCaptionAboveMedia { get; init; }
}

internal sealed class EditMessageCaptionAboveRequest : EditMessageCaptionRequest
{
    public EditMessageCaptionAboveRequest(ChatId chatId, int messageId) : base(chatId, messageId) { }
    [JsonProperty("show_caption_above_media", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowCaptionAboveMedia { get; init; }
}

internal sealed class InputMediaPhotoCaptionAbove : InputMediaPhoto
{
    public InputMediaPhotoCaptionAbove(IInputFile media) : base(media) { }
    [JsonProperty("show_caption_above_media", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ShowCaptionAboveMedia { get; init; }
}
