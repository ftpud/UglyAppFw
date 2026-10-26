using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using File = System.IO.File;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UglyAppFramework.DependencyManager;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyTgApplication;
using UglyTgApplication.Attributes;
using UglyTgApplication.States;
using UglyTgApplication.Services;
using UglyTgApplication.Entity;
using UglyTgApplication.View;

namespace Tests;

public class TelegramFrameworkTests
{
    [Test]
    public async Task MessagesUseChatDestinationAndSessionsAreIsolatedByChatAndUser()
    {
        using var bot = new Harness();
        await bot.Send(-100, 42, "/increment");
        await bot.Send(-100, 43, "/increment");
        await bot.Send(-200, 42, "/increment");
        await bot.Send(-100, 42, "/increment");
        Assert.That(bot.Model(-100, 42).Count, Is.EqualTo(2));
        Assert.That(bot.Model(-100, 43).Count, Is.EqualTo(1));
        Assert.That(bot.Model(-200, 42).Count, Is.EqualTo(1));
        Assert.That(bot.Model(-100, 42).Context.UserId, Is.EqualTo(42));
        Assert.That(bot.Transport.Calls.Where(c => c.Method == "sendMessage")
            .Select(c => JsonDocument.Parse(c.Body).RootElement.GetProperty("chat_id").ToString()),
            Is.EqualTo(new[] { "-100", "-100", "-200" }));
    }

    [Test]
    public async Task AsyncCallbacksFinishBeforeOnMessageAndTheirErrorsPropagate()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "/increment");
        var model = bot.Model(42, 42);
        Assert.That(model.ObservedCount, Is.EqualTo(1));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await bot.Send(42, 42, "/fail"));
        // The session lock must also be released after failure.
        await bot.Send(42, 42, "/increment");
        Assert.That(model.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task RenderIsAwaitedAndSameSessionUpdatesCannotOvertakeIt()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bot.Transport.BeforeResponse = async (method, token) =>
        {
            if (method != "editMessageText") return;
            entered.TrySetResult(true);
            await release.Task.WaitAsync(token);
        };
        var first = bot.Send(42, 42, "/increment");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = bot.Send(42, 42, "/increment");
        try
        {
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(second.IsCompleted, Is.False);
            Assert.That(bot.Model(42, 42).Count, Is.EqualTo(1));
            // Another chat must still make progress.
            await bot.Send(43, 43, "hello").WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { release.TrySetResult(true); }
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(bot.Model(42, 42).Count, Is.EqualTo(2));
    }

    [Test]
    public async Task EditFailureIsObservedAndNextRenderRetries()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        bot.Transport.Error = method => method == "editMessageText" ? "Bad Request: chat not found" : null;
        Assert.ThrowsAsync<ApiRequestException>(async () => await bot.Send(42, 42, "/increment"));
        bot.Transport.Error = null;
        await bot.Model(42, 42).UpdateViewAsync();
        Assert.That(bot.Transport.Calls.Count(c => c.Method == "editMessageText"), Is.EqualTo(2));
    }

    [Test]
    public async Task RepeatedRenderSkipsUnchangedContentAndCanGrowAndShrink()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var model = bot.Model(42, 42);
        await model.UpdateViewAsync();
        Assert.That(bot.Transport.Calls.Select(c => c.Method), Is.EqualTo(new[] { "sendMessage" }));
        model.ExtraMessage = true;
        await model.UpdateViewAsync();
        model.ExtraMessage = false;
        await model.UpdateViewAsync();
        Assert.That(bot.Transport.Calls.Select(c => c.Method),
            Is.EqualTo(new[] { "sendMessage", "sendMessage", "deleteMessage" }));
    }

    [Test]
    public async Task NotModifiedResponseIsHarmlessButOtherErrorsAreNotSwallowed()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        bot.Transport.Error = method => method == "editMessageText" ? "Bad Request: message is not modified" : null;
        await bot.Send(42, 42, "/increment");
        var calls = bot.Transport.Calls.Count;
        await bot.Model(42, 42).UpdateViewAsync();
        Assert.That(bot.Transport.Calls.Count, Is.EqualTo(calls));
    }

    [Test]
    public async Task NonTextAndInlineCallbackUpdatesAreSafeAndCallbacksAreAcknowledged()
    {
        using var bot = new Harness();
        await bot.App.HandleUpdateAsync(bot.Client, new Update(), CancellationToken.None);
        await bot.App.HandleUpdateAsync(bot.Client, new Update { Message = new Message() }, CancellationToken.None);
        await bot.Send(42, 42, null);
        await bot.App.HandleUpdateAsync(bot.Client, new Update
        {
            CallbackQuery = new CallbackQuery { Id = "inline", From = new User { Id = 42 } }
        }, CancellationToken.None);
        await bot.App.HandleUpdateAsync(bot.Client, new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "button", From = new User { Id = 42 }, Data = "/increment",
                Message = new Message { Chat = new Chat { Id = 42 } }
            }
        }, CancellationToken.None);
        Assert.That(bot.Model(42, 42).Count, Is.EqualTo(1));
        Assert.That(bot.Transport.Calls.Count(c => c.Method == "answerCallbackQuery"), Is.EqualTo(2));
    }

    [Test]
    public async Task CancellationReleasesSessionAndReachesNetworkCalls()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bot.Transport.BeforeResponse = async (_, token) =>
        {
            entered.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, token);
        };
        using var cancellation = new CancellationTokenSource();
        var pending = bot.Send(42, 42, "/increment", cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
        bot.Transport.BeforeResponse = null;
        await bot.Send(42, 42, "/increment").WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(bot.Model(42, 42).Count, Is.EqualTo(2));
    }

    [TestCase("Bad Request: query is too old and response timeout expired or query ID is invalid")]
    [TestCase("Bad Request: query ID is invalid")]
    public async Task ExpiredCallbackAcknowledgementDoesNotDiscardButtonAction(string error)
    {
        using var bot = new Harness();
        bot.Transport.Error = method => method == "answerCallbackQuery" ? error : null;
        await bot.App.HandleUpdateAsync(bot.Client, new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "expired", From = new User { Id = 42 }, Data = "/increment",
                Message = new Message { Chat = new Chat { Id = 42 } }
            }
        }, CancellationToken.None);
        Assert.That(bot.Model(42, 42).Count, Is.EqualTo(1));
    }

    [Test]
    public void UnrelatedCallbackAcknowledgementErrorsStillPropagate()
    {
        using var bot = new Harness();
        bot.Transport.Error = method => method == "answerCallbackQuery" ? "Bad Request: unrelated failure" : null;
        Assert.ThrowsAsync<ApiRequestException>(async () => await bot.App.HandleUpdateAsync(bot.Client, new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "button", From = new User { Id = 42 }, Data = "/increment",
                Message = new Message { Chat = new Chat { Id = 42 } }
            }
        }, CancellationToken.None));
        Assert.That(bot.Transport.Calls.Any(c => c.Method == "sendMessage"), Is.False);
    }

    [Test]
    public async Task FirstMessageAfterRestartRemovesAllTrackedViewsBeforeRendering()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ugly-ui-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var first = new Harness(new JsonUiMessageStore(directory)))
            {
                await first.Send(42, 42, "hello");
                var child = new CounterModel { ExtraMessage = true };
                await first.Model(42, 42).PushAsync(child);
            }
            using var restarted = new Harness(new JsonUiMessageStore(directory));
            await restarted.Send(42, 42, "hello");
            Assert.That(restarted.Transport.Calls.Select(c => c.Method),
                Is.EqualTo(new[] { "deleteMessage", "deleteMessage", "deleteMessage", "sendMessage" }));
            await restarted.Send(42, 42, "/increment");
            Assert.That(restarted.Transport.Calls.Count(c => c.Method == "deleteMessage"), Is.EqualTo(3));
            Assert.That(restarted.Model(42, 42).Count, Is.EqualTo(1));
            var remaining = await new JsonUiMessageStore(directory).GetAsync(123456, 42, 42);
            Assert.That(remaining.Count, Is.EqualTo(1));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [Test]
    public async Task CleanupFailureRetainsJournalAndCanBeRetried()
    {
        var store = new InMemoryUiMessageStore();
        await store.AddAsync(123456, 42, 42, new StoredUiMessage(7, DateTimeOffset.UtcNow));
        using var bot = new Harness(store);
        bot.Transport.Error = method => method == "deleteMessage" ? "Bad Request: not enough rights" : null;
        Assert.ThrowsAsync<ApiRequestException>(async () => await bot.Send(42, 42, "hello"));
        Assert.That(bot.Context(42, 42).CurrentState, Is.Null);
        Assert.That((await store.GetAsync(123456, 42, 42)).Single().MessageId, Is.EqualTo(7));
        bot.Transport.Error = null;
        await bot.Send(42, 42, "hello");
        Assert.That(bot.Context(42, 42).CurrentState, Is.Not.Null);
    }

    [TestCase("Bad Request: message to delete not found")]
    [TestCase("Bad Request: message can't be deleted")]
    public async Task MissingOrUndeletablePreviousViewDoesNotBlockFreshSession(string error)
    {
        var store = new InMemoryUiMessageStore();
        await store.AddAsync(123456, 42, 42, new StoredUiMessage(7, DateTimeOffset.UtcNow));
        using var bot = new Harness(store);
        bot.Transport.Error = method => method == "deleteMessage" ? error : null;
        await bot.Send(42, 42, "hello");
        Assert.That(bot.Context(42, 42).CurrentState, Is.Not.Null);
        Assert.That((await store.GetAsync(123456, 42, 42)).Any(m => m.MessageId == 7), Is.False);
    }

    [Test]
    public async Task CleanupLeavesOtherUsersAndBotsAloneAndForgetsExpiredIds()
    {
        var store = new InMemoryUiMessageStore();
        await store.AddAsync(123456, 42, 42, new StoredUiMessage(7, DateTimeOffset.UtcNow.AddDays(-3)));
        await store.AddAsync(123456, 42, 43, new StoredUiMessage(8, DateTimeOffset.UtcNow));
        await store.AddAsync(654321, 42, 42, new StoredUiMessage(9, DateTimeOffset.UtcNow));
        using var bot = new Harness(store);
        await bot.Send(42, 42, "hello");
        Assert.That(bot.Transport.Calls.Select(c => c.Method), Is.EqualTo(new[] { "sendMessage" }));
        Assert.That((await store.GetAsync(123456, 42, 43)).Single().MessageId, Is.EqualTo(8));
        Assert.That((await store.GetAsync(654321, 42, 42)).Single().MessageId, Is.EqualTo(9));
        Assert.That((await store.GetAsync(123456, 42, 42)).Any(m => m.MessageId == 7), Is.False);
    }

    [Test]
    public async Task JsonStoreDoesNotOverwriteCorruptData()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ugly-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "123456-42-42.json");
            await File.WriteAllTextAsync(path, "invalid journal");
            var store = new JsonUiMessageStore(directory);
            Assert.ThrowsAsync<JsonException>(async () => await store.AddAsync(123456, 42, 42,
                new StoredUiMessage(7, DateTimeOffset.UtcNow)));
            Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo("invalid journal"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Test]
    public async Task NavigationAwaitsCleanupAndPoppingRootIsSafe()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var root = bot.Model(42, 42);
        await root.PopAsync();
        Assert.That(bot.Context(42, 42).CurrentState, Is.SameAs(root));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await root.PushAsync(root));
        var child = new CounterModel();
        await root.PushAsync(child);
        child.ExtraMessage = true;
        await child.UpdateViewAsync();
        await child.PopAsync();
        Assert.That(bot.Context(42, 42).CurrentState, Is.SameAs(root));
        Assert.That(bot.Transport.Calls.Count(c => c.Method == "deleteMessage"), Is.EqualTo(2));
        await bot.Sessions.ResetSessionAsync(42, 42);
        Assert.That(bot.Context(42, 42).CurrentState, Is.Null);
        await bot.Send(42, 42, "/increment");
        Assert.That(bot.Model(42, 42), Is.Not.SameAs(root));
        Assert.That(bot.Model(42, 42).Count, Is.EqualTo(1));
    }

    [Test]
    public async Task InitializationFailureRestoresParentAndCleansPartialView()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var root = bot.Model(42, 42);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await root.PushAsync(new FailingModel()));
        Assert.That(bot.Context(42, 42).CurrentState, Is.SameAs(root));
        Assert.That(bot.Transport.Calls.Last().Method, Is.EqualTo("deleteMessage"));
    }

    [Test]
    public async Task RegisteredAsyncCallbackNavigatesWithoutCallingInactiveMessageHook()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var root = bot.Model(42, 42);
        var child = new CounterModel();
        var calls = root.HookCalls;
        root.RegisterCallback("/next", async (_, token) =>
        {
            await Task.Yield();
            await root.PushAsync(child, token);
        });
        await bot.Send(42, 42, "/next");
        Assert.That(bot.Context(42, 42).CurrentState, Is.SameAs(child));
        Assert.That(root.HookCalls, Is.EqualTo(calls));
    }

    [Test]
    public async Task FailedDeleteKeepsNavigationStateForRetry()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var root = bot.Model(42, 42);
        var child = new CounterModel();
        await root.PushAsync(child);
        bot.Transport.Error = method => method == "deleteMessage" ? "Bad Request: not enough rights" : null;
        Assert.ThrowsAsync<ApiRequestException>(async () => await child.PopAsync());
        Assert.That(bot.Context(42, 42).CurrentState, Is.SameAs(child));
        bot.Transport.Error = null;
        await child.PopAsync();
        Assert.That(bot.Context(42, 42).CurrentState, Is.SameAs(root));
    }

    [Test]
    public async Task ConcurrentDependencyResolutionKeepsOneSingleton()
    {
        var dependencies = new DependencyManager();
        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
            dependencies.GetDependencyInstance(typeof(ConcurrentService), new InjectAttribute()))));
        Assert.That(results.All(instance => ReferenceEquals(instance, results[0])), Is.True);
    }

    [Test]
    public async Task RefreshQueueCoalescesRendersOnlyVisibleViewsAndSkipsDetachedOnes()
    {
        using var bot = new Harness();
        await bot.Send(42, 42, "hello");
        var model = bot.Model(42, 42);
        model.Bump();
        bot.Refresh.Request(model);
        bot.Refresh.Request(model);
        bot.Refresh.Request(new CounterModel()); // never pushed: no session to render into
        Assert.That(bot.Refresh.PendingCount, Is.EqualTo(2));
        var before = bot.Transport.Calls.Count;
        await bot.Refresh.ProcessPendingAsync();
        Assert.That(bot.Refresh.PendingCount, Is.Zero);
        Assert.That(bot.Transport.Calls.Skip(before).Select(c => c.Method), Is.EqualTo(new[] { "editMessageText" }));

        var child = new CounterModel();
        await model.PushAsync(child);
        model.Bump();
        bot.Refresh.Request(model);
        before = bot.Transport.Calls.Count;
        await bot.Refresh.ProcessPendingAsync();
        Assert.That(bot.Transport.Calls.Count, Is.EqualTo(before), "covered pages wait for reactivation");
    }

    [Test]
    public void AsyncVoidAndInvalidCallbackSignaturesAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new AsyncVoidModel());
        Assert.Throws<InvalidOperationException>(() => new InvalidCallbackModel());
    }

    private sealed class Harness : IDisposable
    {
        public FakeTransport Transport { get; } = new();
        private readonly HttpClient _http;
        public TelegramBotClient Client { get; }
        public UglyTgApp App { get; }
        public SessionManager Sessions { get; }
        public ViewRefreshQueue Refresh { get; }

        public Harness(IUiMessageStore? store = null)
        {
            _http = new HttpClient(Transport);
            Client = new TelegramBotClient("123456:offline-test-token", _http);
            var dependencies = new DependencyManager();
            App = new UglyTgApp("123456:offline-test-token", typeof(CounterModel));
            dependencies.InjectDependencies(App);
            var interaction = (TelegramInteractionManager)dependencies.GetDependencyInstance(typeof(TelegramInteractionManager), new InjectAttribute());
            interaction._telegramBotClient = Client;
            if (store != null) interaction.MessageStore = store;
            Sessions = (SessionManager)dependencies.GetDependencyInstance(typeof(SessionManager), new InjectAttribute());
            Refresh = (ViewRefreshQueue)dependencies.GetDependencyInstance(typeof(ViewRefreshQueue), new InjectAttribute());
        }

        public UglyTgApplication.Entity.SessionContext Context(long chat, long user) => Sessions.GetCurrentContext(chat, user);
        public CounterModel Model(long chat, long user) => (CounterModel)Context(chat, user).CurrentState!;
        public Task Send(long chat, long user, string? text, CancellationToken token = default) =>
            App.HandleUpdateAsync(Client, new Update
            {
                Message = new Message
                {
                    MessageId = 99, Chat = new Chat { Id = chat }, From = new User { Id = user }, Text = text
                }
            }, token);
        public void Dispose() => _http.Dispose();
    }

    private sealed class FakeTransport : HttpMessageHandler
    {
        private int _messageId;
        public ConcurrentQueue<(string Method, string Body)> Calls { get; } = new();
        public Func<string, CancellationToken, Task>? BeforeResponse { get; set; }
        public Func<string, string?>? Error { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var method = request.RequestUri!.Segments.Last();
            Calls.Enqueue((method, await request.Content!.ReadAsStringAsync(token)));
            if (BeforeResponse != null) await BeforeResponse(method, token);
            var error = Error?.Invoke(method);
            var result = method is "deleteMessage" or "answerCallbackQuery" ? "true"
                : "{\"message_id\":" + Interlocked.Increment(ref _messageId) + ",\"date\":0,\"chat\":{\"id\":42,\"type\":\"private\"}}";
            return new HttpResponseMessage(error == null ? HttpStatusCode.OK : HttpStatusCode.BadRequest)
            {
                Content = new StringContent(error == null ? "{\"ok\":true,\"result\":" + result + "}"
                    : JsonSerializer.Serialize(new { ok = false, error_code = 400, description = error }), Encoding.UTF8, "application/json")
            };
        }
    }

    [BindView(typeof(CounterView))]
    public class CounterModel : UglyTgApplication.States.ViewModel
    {
        public int Count { get; private set; }
        public int ObservedCount { get; private set; }
        public int HookCalls { get; private set; }
        public bool ExtraMessage { get; set; }
        public CounterModel() => RemoveUserMessagesPolicyEnabled = false;
        public void Bump() => Count++;

        [Callback(Trigger = "/increment")]
        public async Task Increment(Update update, CancellationToken token)
        {
            await Task.Yield();
            Count++;
            await UpdateViewAsync(token);
        }

        [Callback(Trigger = "/fail")]
        public async Task Fail(Update update)
        {
            await Task.Yield();
            throw new InvalidOperationException("callback failed");
        }

        public override Task OnMessageAsync(Update update, CancellationToken cancellationToken = default)
        {
            ObservedCount = Count;
            HookCalls++;
            return Task.CompletedTask;
        }
    }

    [Managed]
    public class CounterView : IView
    {
        public ViewResponse Display(IState state)
        {
            var model = (CounterModel)state;
            return new ViewResponse
            {
                ResponseMessages = model.ExtraMessage
                    ? new[] { new ResponseData { text = model.Count.ToString() }, new ResponseData { text = "extra" } }
                    : new[] { new ResponseData { text = model.Count.ToString() } }
            };
        }
    }

    public class FailingModel : CounterModel
    {
        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await base.InitializeAsync(cancellationToken);
            throw new InvalidOperationException("initialization failed");
        }
    }

    public class AsyncVoidModel : CounterModel
    {
        [Callback(Trigger = "/bad")]
        public async void Bad(Update update) => await Task.Yield();
    }

    public class InvalidCallbackModel : CounterModel
    {
        [Callback(Trigger = "/bad")]
        public int Bad() => 0;
    }

    [Managed]
    public class ConcurrentService { }
}
