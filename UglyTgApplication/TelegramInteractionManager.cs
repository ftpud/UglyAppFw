using System.Text.Json;
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
    [Inject] private DependencyManager DependencyManager { get; set; } = null!;

    public void Load(string token) => _telegramBotClient = new TelegramBotClient(token);

    internal async Task DisplayViewAsync(SessionContext context, TgState state,
        CancellationToken cancellationToken = default)
    {
        var binding = state.GetType().GetCustomAttributes(typeof(BindViewAttribute), true)
            .Cast<BindViewAttribute>().SingleOrDefault()
            ?? throw new InvalidOperationException("View is expected to be attached to ViewModel");
        var view = (IView)DependencyManager.GetDependencyInstance(binding.ViewType, new InjectAttribute());
        var responses = view.Display(state).ResponseMessages;
        for (var i = 0; i < responses.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = responses[i];
            var fingerprint = JsonSerializer.Serialize(new { response.text, response.parseMode, response.replyMarkup });
            if (i >= state.Messages.Count)
            {
                var message = await _telegramBotClient.SendTextMessageAsync(context.CurrentChatId, response.text,
                    parseMode: response.parseMode, replyMarkup: response.replyMarkup, cancellationToken: cancellationToken);
                state.Messages.Add((message.MessageId, fingerprint));
                await MessageStore.AddAsync(BotId, context.CurrentChatId.Identifier!.Value, context.UserId,
                    new StoredUiMessage(message.MessageId, DateTimeOffset.UtcNow), cancellationToken);
            }
            else if (state.Messages[i].Fingerprint != fingerprint)
            {
                try
                {
                    await _telegramBotClient.EditMessageTextAsync(context.CurrentChatId, state.Messages[i].Id,
                        response.text, parseMode: response.parseMode, replyMarkup: response.replyMarkup,
                        cancellationToken: cancellationToken);
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400 &&
                    ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                {
                    // Telegram may normalize two different payloads to identical content.
                }
                state.Messages[i] = (state.Messages[i].Id, fingerprint);
            }
        }
        while (state.Messages.Count > responses.Length)
        {
            var last = state.Messages.Count - 1;
            await RemoveMessageAsync(context, state.Messages[last].Id, cancellationToken);
            state.Messages.RemoveAt(last);
        }
    }

    internal async Task RemoveMessageAsync(SessionContext context, int messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _telegramBotClient.DeleteMessageAsync(context.CurrentChatId, messageId, cancellationToken);
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
