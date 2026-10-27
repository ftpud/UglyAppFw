using System.Text.Json;
using System.Security.Cryptography;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using UglyAppFramework.DependencyManager;
using UglyAppFramework.DependencyManager.Attributes;
using UglyTgApplication.Attributes;
using UglyTgApplication.Entity;
using UglyTgApplication.States;
using UglyTgApplication.Services;
using UglyTgApplication.View;

namespace UglyTgApplication;

[Managed]
public class TelegramInteractionManager
{
    public ITelegramBotClient _telegramBotClient { get; set; } = null!;
    public IUiMessageStore MessageStore { get; set; } = new InMemoryUiMessageStore();
    private long BotId => _telegramBotClient.BotId is { } id ? id
        : throw new InvalidOperationException("A bot ID is required to track UI messages.");
    [Inject] private OutboundScheduler Scheduler { get; set; } = null!;
    private readonly object _photoSync = new();
    private readonly Dictionary<string, string> _photoIds = new();
    private readonly Queue<string> _photoOrder = new();
    [Inject] private DependencyManager DependencyManager { get; set; } = null!;

    public void Load(string token)
    {
        lock (_photoSync) { _photoIds.Clear(); _photoOrder.Clear(); }
        _telegramBotClient = new TelegramBotClient(token);
    }

    internal async Task DisplayViewAsync(SessionContext context, TgState state,
        CancellationToken cancellationToken = default)
    {
        var binding = state.GetType().GetCustomAttributes(typeof(BindViewAttribute), true)
            .Cast<BindViewAttribute>().SingleOrDefault()
            ?? throw new InvalidOperationException("View is expected to be attached to ViewModel");
        var view = (IView)DependencyManager.GetDependencyInstance(binding.ViewType, new InjectAttribute());
        var responses = view.Display(state).ResponseMessages;
        var keyed = responses.Any(response => response.Key != null);
        if (keyed && (responses.Any(response => string.IsNullOrEmpty(response.Key)) ||
            responses.Select(response => response.Key).Distinct().Count() != responses.Length))
            throw new InvalidOperationException("A keyed page requires unique, non-empty keys on every message.");
        // Drop removed identities first. Existing messages keep their Telegram IDs.
        // If new identities are inserted or reordered, re-send the affected suffix
        // because Telegram cannot insert a message before an existing one.
        if (state.Messages.Any(message => (message.Key != null) != keyed))
        {
            while (state.Messages.Count > 0)
            {
                var last = state.Messages.Count - 1;
                await RemoveMessageAsync(context, state.Messages[last].Id, cancellationToken);
                state.Messages.RemoveAt(last);
            }
        }
        if (keyed)
        {
            var keys = responses.Select(response => response.Key).ToHashSet();
            for (var i = state.Messages.Count - 1; i >= 0; i--)
                if (!keys.Contains(state.Messages[i].Key))
                {
                    await RemoveMessageAsync(context, state.Messages[i].Id, cancellationToken);
                    state.Messages.RemoveAt(i);
                }
            var shared = Math.Min(state.Messages.Count, responses.Length);
            var mismatch = 0;
            while (mismatch < shared && state.Messages[mismatch].Key == responses[mismatch].Key) mismatch++;
            while (state.Messages.Count > mismatch)
            {
                var last = state.Messages.Count - 1;
                await RemoveMessageAsync(context, state.Messages[last].Id, cancellationToken);
                state.Messages.RemoveAt(last);
            }
        }
        for (var i = 0; i < responses.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = responses[i];
            var isPhoto = response.Photo != null;
            var photoHash = isPhoto ? Convert.ToHexString(SHA256.HashData(response.Photo!)) : null;
            var fingerprint = JsonSerializer.Serialize(new { response.text, response.parseMode, response.replyMarkup, photo = photoHash });
            // Telegram cannot convert a text message into a photo or vice versa. Remove
            // this message and later siblings so re-sends preserve their order.
            if (i < state.Messages.Count && state.Messages[i].IsPhoto != isPhoto)
            {
                while (state.Messages.Count > i)
                {
                    var last = state.Messages.Count - 1;
                    await RemoveMessageAsync(context, state.Messages[last].Id, cancellationToken);
                    state.Messages.RemoveAt(last);
                }
            }
            if (i >= state.Messages.Count)
            {
                Message message;
                var chat = context.CurrentChatId.Identifier!.Value;
                if (isPhoto)
                {
                    message = await SendPhotoAsync(context.CurrentChatId, response, photoHash!, cancellationToken);
                }
                else
                    message = await Scheduler.ExecuteAsync(ct => _telegramBotClient.SendTextMessageAsync(context.CurrentChatId, response.text,
                        parseMode: response.parseMode, replyMarkup: response.replyMarkup, cancellationToken: ct), chat, true, cancellationToken);
                state.Messages.Add((message.MessageId, fingerprint, isPhoto, response.Key, photoHash));
                await MessageStore.AddAsync(BotId, context.CurrentChatId.Identifier!.Value, context.UserId,
                    new StoredUiMessage(message.MessageId, DateTimeOffset.UtcNow), cancellationToken);
            }
            else if (state.Messages[i].Fingerprint != fingerprint)
            {
                try
                {
                    var chat = context.CurrentChatId.Identifier!.Value;
                    if (isPhoto && state.Messages[i].PhotoHash == photoHash)
                        await Scheduler.ExecuteAsync(ct => _telegramBotClient.EditMessageCaptionAsync(context.CurrentChatId, state.Messages[i].Id,
                            caption: response.text, parseMode: response.parseMode, replyMarkup: response.replyMarkup, cancellationToken: ct),
                            chat, false, cancellationToken);
                    else if (isPhoto)
                        await EditPhotoAsync(context.CurrentChatId, state.Messages[i].Id, response, photoHash!, cancellationToken);
                    else
                        await Scheduler.ExecuteAsync(ct => _telegramBotClient.EditMessageTextAsync(context.CurrentChatId, state.Messages[i].Id,
                            response.text, parseMode: response.parseMode, replyMarkup: response.replyMarkup, cancellationToken: ct),
                            chat, false, cancellationToken);
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400 &&
                    ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase)) { }
                state.Messages[i] = (state.Messages[i].Id, fingerprint, isPhoto, response.Key, photoHash);
            }
        }
        while (state.Messages.Count > responses.Length)
        {
            var last = state.Messages.Count - 1;
            await RemoveMessageAsync(context, state.Messages[last].Id, cancellationToken);
            state.Messages.RemoveAt(last);
        }
    }

    private string? CachedPhoto(string hash) { lock (_photoSync) return _photoIds.GetValueOrDefault(hash); }
    private void ForgetPhoto(string hash) { lock (_photoSync) _photoIds.Remove(hash); }
    private void RememberPhoto(string hash, Message message)
    {
        var id = message.Photo?.LastOrDefault()?.FileId;
        if (string.IsNullOrEmpty(id)) return;
        lock (_photoSync)
        {
            if (!_photoIds.ContainsKey(hash)) _photoOrder.Enqueue(hash);
            _photoIds[hash] = id;
            while (_photoOrder.Count > 256) _photoIds.Remove(_photoOrder.Dequeue());
        }
    }
    private static bool InvalidFileId(ApiRequestException ex) => ex.ErrorCode == 400 &&
        (ex.Message.Contains("wrong file identifier", StringComparison.OrdinalIgnoreCase) ||
         ex.Message.Contains("file reference", StringComparison.OrdinalIgnoreCase));

    private async Task<Message> SendPhotoAsync(ChatId chat, ResponseData response, string hash, CancellationToken token)
    {
        async Task<Message> Send(bool cached)
        {
            return await Scheduler.ExecuteAsync(async ct =>
            {
                var id = cached ? CachedPhoto(hash) : null;
                if (id != null)
                    return await _telegramBotClient.SendPhotoAsync(chat, new InputFileId(id), caption: response.text,
                        parseMode: response.parseMode, replyMarkup: response.replyMarkup, cancellationToken: ct);
                using var stream = new MemoryStream(response.Photo!, writable: false);
                return await _telegramBotClient.SendPhotoAsync(chat, new InputFile(stream, "card.png"), caption: response.text,
                    parseMode: response.parseMode, replyMarkup: response.replyMarkup, cancellationToken: ct);
            }, chat.Identifier!.Value, true, token);
        }
        Message message;
        try { message = await Send(true); }
        catch (ApiRequestException ex) when (InvalidFileId(ex) && CachedPhoto(hash) != null)
        { ForgetPhoto(hash); message = await Send(false); }
        RememberPhoto(hash, message);
        return message;
    }

    private async Task EditPhotoAsync(ChatId chat, int messageId, ResponseData response, string hash, CancellationToken token)
    {
        async Task<Message> Edit(bool cached) => await Scheduler.ExecuteAsync(async ct =>
        {
            var id = cached ? CachedPhoto(hash) : null;
            using var stream = id == null ? new MemoryStream(response.Photo!, writable: false) : null;
            IInputFile input = id == null ? new InputFile(stream!, "card.png") : new InputFileId(id);
            var media = new InputMediaPhoto(input) { Caption = response.text, ParseMode = response.parseMode };
            return await _telegramBotClient.EditMessageMediaAsync(chat, messageId, media, replyMarkup: response.replyMarkup, cancellationToken: ct);
        }, chat.Identifier!.Value, false, token);
        Message message;
        try { message = await Edit(true); }
        catch (ApiRequestException ex) when (InvalidFileId(ex) && CachedPhoto(hash) != null)
        { ForgetPhoto(hash); message = await Edit(false); }
        RememberPhoto(hash, message);
    }

    internal async Task RemoveMessageAsync(SessionContext context, int messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Scheduler.ExecuteAsync(ct => _telegramBotClient.DeleteMessageAsync(context.CurrentChatId, messageId, ct),
                context.CurrentChatId.Identifier!.Value, false, cancellationToken);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400 &&
            ex.Message.Contains("message to delete not found", StringComparison.OrdinalIgnoreCase))
        {
            // Already removed externally; unloading can still finish.
        }
        await MessageStore.RemoveAsync(BotId, context.CurrentChatId.Identifier!.Value, context.UserId,
            messageId, cancellationToken);
    }

    internal async Task CleanPreviousViewsAsync(SessionContext context, CancellationToken cancellationToken)
    {
        var chatId = context.CurrentChatId.Identifier!.Value;
        var previous = await MessageStore.GetAsync(BotId, chatId, context.UserId, cancellationToken);
        foreach (var message in previous)
        {
            if (DateTimeOffset.UtcNow - message.SentAt < TimeSpan.FromHours(48))
            {
                try
                {
                    await RemoveMessageAsync(context, message.MessageId, cancellationToken);
                    continue;
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400 &&
                    ex.Message.Contains("message can't be deleted", StringComparison.OrdinalIgnoreCase))
                {
                    // Telegram will not permit removing this old view; do not block a fresh session.
                }
            }
            await MessageStore.RemoveAsync(BotId, chatId, context.UserId, message.MessageId, cancellationToken);
        }
    }
}
