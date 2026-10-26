using System.Text.Json;
using UglyTgApplication.Entity;

namespace UglyTgApplication.Services;

// One store per process; run one polling instance for a bot token.
public class JsonUiMessageStore : IUiMessageStore
{
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonUiMessageStore(string directory) => _directory = Path.GetFullPath(directory);

    public async Task<IReadOnlyList<StoredUiMessage>> GetAsync(long botId, long chatId, long userId, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try { return await ReadAsync(PathFor(botId, chatId, userId), token); }
        finally { _gate.Release(); }
    }

    public Task AddAsync(long botId, long chatId, long userId, StoredUiMessage message, CancellationToken token = default) =>
        ChangeAsync(botId, chatId, userId, messages =>
        {
            messages.RemoveAll(item => item.MessageId == message.MessageId);
            messages.Add(message);
        }, token);

    public Task RemoveAsync(long botId, long chatId, long userId, int messageId, CancellationToken token = default) =>
        ChangeAsync(botId, chatId, userId, messages => messages.RemoveAll(item => item.MessageId == messageId), token);

    private string PathFor(long botId, long chatId, long userId) =>
        Path.Combine(_directory, $"{botId}-{chatId}-{userId}.json");

    private static async Task<List<StoredUiMessage>> ReadAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path)) return new();
        await using var input = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<StoredUiMessage>>(input, cancellationToken: token)
            ?? throw new InvalidDataException("The UI message journal contains an invalid null document.");
    }

    private async Task ChangeAsync(long botId, long chatId, long userId, Action<List<StoredUiMessage>> change,
        CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var path = PathFor(botId, chatId, userId);
            var messages = await ReadAsync(path, token);
            change(messages);
            if (messages.Count == 0)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            Directory.CreateDirectory(_directory);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(messages), token);
                File.Move(temporary, path, overwrite: true);
            }
            finally { File.Delete(temporary); }
        }
        finally { _gate.Release(); }
    }
}
