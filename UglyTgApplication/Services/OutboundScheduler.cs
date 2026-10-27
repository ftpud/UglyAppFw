using System.Diagnostics;
using Telegram.Bot.Exceptions;
using UglyAppFramework.DependencyManager.Attributes;

namespace UglyTgApplication.Services;

// Shared token buckets: all UI traffic has a global budget, and sends also have
// a small per-chat burst. Waiters never hold a global lock while delaying or I/O.
[Managed]
public sealed class OutboundScheduler
{
    private sealed class Bucket { public double Tokens; public double Updated; }
    private readonly object _sync = new();
    private readonly Bucket _global = new() { Tokens = 25, Updated = Now };
    private readonly Dictionary<long, Bucket> _chats = new();
    private double _cooldownUntil;
    private static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    public bool Enabled { get; set; }
    public int MaxRetries { get; set; } = 3;

    private static void Refill(Bucket bucket, double now, double rate, double capacity)
    {
        bucket.Tokens = Math.Min(capacity, bucket.Tokens + Math.Max(0, now - bucket.Updated) * rate);
        bucket.Updated = now;
    }
    private async Task WaitAsync(long? chatId, bool isSend, CancellationToken token)
    {
        while (true)
        {
            double delay;
            lock (_sync)
            {
                var now = Now;
                Refill(_global, now, 25, 25);
                Bucket? chat = null;
                if (isSend && chatId.HasValue)
                {
                    if (!_chats.TryGetValue(chatId.Value, out chat)) _chats[chatId.Value] = chat = new Bucket { Tokens = 3, Updated = now };
                    Refill(chat, now, 1, 3);
                }
                delay = Math.Max(0, _cooldownUntil - now);
                delay = Math.Max(delay, Math.Max(0, (1 - _global.Tokens) / 25));
                if (chat != null) delay = Math.Max(delay, Math.Max(0, 1 - chat.Tokens));
                if (delay <= 0)
                {
                    _global.Tokens -= 1;
                    if (chat != null) chat.Tokens -= 1;
                    // Bound idle chat bookkeeping without retaining every user forever.
                    if (_chats.Count > 4096)
                        foreach (var id in _chats.Where(p => now - p.Value.Updated > 600).Select(p => p.Key).ToArray()) _chats.Remove(id);
                    return;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(delay, 0.001)), token);
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> request, long? chatId,
        bool isSend, CancellationToken token = default, bool pace = true)
    {
        for (var attempt = 0; ; attempt++)
        {
            token.ThrowIfCancellationRequested();
            if (Enabled && pace) await WaitAsync(chatId, isSend, token);
            try { return await request(token); }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429 && attempt < MaxRetries)
            {
                var delay = Math.Max(1, ex.Parameters?.RetryAfter ?? 1);
                if (Enabled) lock (_sync) _cooldownUntil = Math.Max(_cooldownUntil, Now + delay);
                await Task.Delay(TimeSpan.FromSeconds(delay), token);
            }
        }
    }
    public async Task ExecuteAsync(Func<CancellationToken, Task> request, long? chatId,
        bool isSend, CancellationToken token = default, bool pace = true) =>
        await ExecuteAsync(async ct => { await request(ct); return true; }, chatId, isSend, token, pace);
}
