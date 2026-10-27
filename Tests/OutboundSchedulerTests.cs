using System.Diagnostics;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using UglyTgApplication.Services;

namespace Tests;

public class OutboundSchedulerTests
{
    [Test] public async Task ChatBurstIsPacedButAnotherChatCanProceedAndWaitingCanCancel()
    {
        var scheduler = new OutboundScheduler { Enabled = true };
        for (var i = 0; i < 3; i++) await scheduler.ExecuteAsync(_ => Task.CompletedTask, 1, true);
        using var cancel = new CancellationTokenSource();
        var waiting = scheduler.ExecuteAsync(_ => Task.CompletedTask, 1, true, cancel.Token);
        await Task.Delay(50);
        Assert.That(waiting.IsCompleted, Is.False);
        await scheduler.ExecuteAsync(_ => Task.CompletedTask, 2, true).WaitAsync(TimeSpan.FromSeconds(1));
        cancel.Cancel();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await waiting);
    }

    [Test] public async Task RetryAfterIsRespectedAndNonRateErrorsAreNotRetried()
    {
        var scheduler = new OutboundScheduler();
        var attempts = 0;
        var watch = Stopwatch.StartNew();
        var result = await scheduler.ExecuteAsync<int>(_ =>
        {
            if (++attempts == 1) throw new ApiRequestException("slow down", 429, new ResponseParameters { RetryAfter = 1 });
            return Task.FromResult(7);
        }, 1, true);
        Assert.That(result, Is.EqualTo(7));
        Assert.That(attempts, Is.EqualTo(2));
        Assert.That(watch.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900)));
        attempts = 0;
        Assert.ThrowsAsync<ApiRequestException>(async () => await scheduler.ExecuteAsync<int>(_ =>
        { attempts++; throw new ApiRequestException("bad", 400); }, 1, false));
        Assert.That(attempts, Is.EqualTo(1));
    }
}
