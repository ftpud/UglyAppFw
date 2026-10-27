# UglyAppFw
Kind of ugly UI framework for creating a telegram bot.

Currently supports:
+ Isolated multi-user state-based interactions
+ MVVM architecture
+ DI and IoC container

Reusable dialogs and form controls live in the sibling `../UglyFwUI` project.
`UglyDemo` consumes that library; it no longer contains private copies of the
dialogs. Application presentation folders are `Views/`, `ViewModels/`, `Models/`
and `Services/`. See `../ARCHITECTURE.md` for the workspace layout and
`../UglyWorkspace.sln` to build/test the framework, shared UI and both examples.

## Async API

This revision changes the state/navigation API. Update existing applications to
await navigation, callbacks and rendering; the included ToDo demo and the sibling
`TelegramTestBot` application use the new methods.

| Previous method | Replacement |
| --- | --- |
| `Push(state)` / `Pop()` | `await PushAsync(state, token)` / `await PopAsync(token)` |
| `Initialize`, `Activate`, `Unload` | Override `Task InitializeAsync`, `ActivateAsync`, `DeactivateAsync`, `UnloadAsync` |
| `Update(update)` / `OnMessage(update)` | `UpdateAsync(update, token)` / override `OnMessageAsync` |
| `UpdateView()` | `await UpdateViewAsync(token)` |
| `PushState` / `PopState` | `PushStateAsync` / `PopStateAsync` |

Async overrides take an optional `CancellationToken cancellationToken = default`.
`SetProperty` now only changes the field and returns whether it changed. Render
explicitly with `await UpdateViewAsync(token)` from an async handler; property
setters cannot await network requests.

Attributed callbacks accept `Update` and optionally `CancellationToken`. Return
`Task` for async work (synchronous `void` handlers still work). `async void` and
unsupported signatures are rejected when constructing the view model:

```csharp
[Callback(Trigger = "/next")]
public async Task Next(Update update, CancellationToken cancellationToken)
{
    await PushAsync(new NextPageViewModel(), cancellationToken);
}
```

For dynamic callbacks use
`RegisterCallback("next", async (update, token) => await PushAsync(next, token))`.
The dispatcher awaits callbacks before `OnMessageAsync`, and skips the old
state's message hook when a callback navigates away.

Telegram sends, edits and deletes are awaited and receive cancellation tokens.
Failures propagate to the polling error handler (or to the caller when dispatching
directly). Identical renders are skipped; views may grow or shrink their message
count. Only Telegram's specific "message is not modified" and "message to delete
not found" errors are treated as harmless. Expired/invalid callback-query
acknowledgements also do not prevent the button action from being dispatched.
Other errors remain visible.

`UglyTgApp.Load` enables an outbound scheduler shared by the bot: UI operations
are paced at 25 per second with a burst of 25, and sends also have a per-chat
budget of one per second with a burst of three. Callback acknowledgements bypass
this pacing. Telegram 429 responses are retried up to three times after their
`retry_after` delay, with a shared cooldown. Waiting is cancellable. These budgets
are per process and are not coordinated across replicas. Offline direct dispatch
does not enable pacing unless the injected `OutboundScheduler` is enabled.

## Photo responses

`ResponseData.Photo` optionally contains encoded image bytes; `text` is then the
HTML caption (Telegram allows up to 1024 caption characters). Without `Photo`,
responses remain text messages. Photo responses support inline keyboards, are
tracked for cleanup, and are edited when their contents change. Switching a
response between text and photo removes and re-sends it and later sibling
messages to preserve page order. Prepare image bytes before rendering: views
remain pure and should not perform file or network I/O.
Unchanged photo bytes use caption-only edits. The transport caches up to 256
Telegram file IDs by content hash in memory, clearing them on `Load`; an invalid
cached file ID falls back to uploading the image again.

## Keyed messages

By default, page messages are matched by position. For pages with independently
removable messages, set a unique non-empty `ResponseData.Key` on every response
in the page. Removing a key deletes that exact message and retains other message
IDs. New or reordered keys may require re-sending a suffix because Telegram
cannot insert messages before existing ones. Do not mix keyed and unkeyed
responses in a page. A callback from a deleted message is ignored when the focused
page is keyed, so a stale button cannot act on a later version of that page.

## Push modes

`PushAsync(state, mode, token)` takes a `PushMode`; `PushAsync(state, token)` uses `Replace`.

| Mode | Screen | Input |
| --- | --- | --- |
| `Replace` (standard) | The new page replaces everything visible. Hidden pages get `DeactivateAsync` (messages deleted, state kept) and are sent again, in order and with their current state, when it is popped. | The new page |
| `Panel` | Sent as extra messages after the visible pages, which stay visible and interactive. Has its own lifecycle: `panel.PopAsync()` closes just it (and anything above it). | Its own buttons; typed text keeps going to the page below |
| `Modal` | Sent after the visible pages, which stay visible (and keep live-updating) but are blocked. When it is popped they are edited in place, not re-sent. | Typed text and its own buttons; clicks on pages behind it get a `BlockedInputNotice` toast |

The visible pages are the top of the stack plus everything below it down to the
first page pushed with `Replace`. Each button press is routed to the visible page
that owns the pressed message. Typed text goes to the *focused* page: the topmost
visible page that is not a panel. `StateManager.GetVisibleStates`,
`GetFocusedState` and `IsBlocked` expose these rules. `ViewRefreshQueue` renders
every visible page, including pages behind a modal.

`state.PopAsync()` closes *that* state together with every state above it, then
activates whatever becomes visible (bottom-up). Popping the root is still a no-op.
Pages pushed onto the stack always go on top, even when a lower panel pushes them.
A lower page whose message count grows while a panel is open sends its new
messages after the panel.

A page may render several messages, each with its own keyboard
(`ViewResponse.ResponseMessages`). All of them belong to one view model, are
diffed separately (unchanged messages are not edited) and are hidden and restored
together.

## Dependency injection

`[Managed]` types are registered from all loaded assemblies. Singletons are the
default. `[Managed(Scope = DependencyEntity.DependencyScope.Prototype)]` creates a
fresh, injected instance for every injection point. An `[Inject]` property
resolves by exact type, by `Identifier`, or else by the single concrete
`[Managed]` implementation of an interface or base type. Objects are injected
once; `[PostConstruct]` methods run after their injection.

States are injected when they are constructed. `UglyTgApp` opens a
`DependencyManager.Enter()` scope for each update, and `State`'s constructor
injects itself from `DependencyManager.Current`. So a view model created in a
handler (including the starting state) can use its `[Inject]` services in its
own constructor. States constructed outside a scope (e.g. directly in tests) are
injected when pushed, as before. A `[PostConstruct]` method on a state runs
before the derived constructor body; prefer the constructor there.

## Sessions and lifecycle

`SessionManager.GetCurrentContext(chatId, userId)` keys state by both destination
chat and sender. `Context.CurrentChatId` is the rendering destination;
`Context.UserId` / `CurrentUserId` identify the sender. The old
`getCurrentContext(userId)` overload is obsolete and assumes a private chat.
This isolates different users in a group and the same user across chats.

The dispatcher serializes each session's initialization, callbacks and rendering;
other sessions can progress independently. Use navigation/render methods within
awaited handlers. Non-text updates are safe to dispatch; message hooks should
check `update.Message?.Text` before consuming text. Inline-mode callback queries
without a message are acknowledged and ignored because they have no chat view.

Popping a root state is a no-op. Failed initialization restores the parent and
attempts to remove partially sent views. From outside an update handler, call
`await sessions.ResetSessionAsync(chatId, userId, token)` to unload the navigation
stack. Do not call reset from a session callback: it takes the same session lock.
Contexts remain in memory to preserve locks for queued updates; there is no
automatic expiry or persistence.

UI message IDs can be journaled separately from application state. Pass an
`IUiMessageStore` as the optional `messageStore` argument to `UglyTgApp.Start`
or its constructor. The default `InMemoryUiMessageStore` lasts for the process;
`new JsonUiMessageStore(directory)` persists only message IDs and sent timestamps,
keyed by bot, chat and user. It never stores tokens or message text. Use one
polling instance per bot token and one store per process.

On the first update of a new session, tracked views are cleaned before the
starting state is rendered. Sending/removing views updates the journal, so a
restart can clean up the whole previous navigation stack. Missing messages or
messages Telegram refuses to delete are forgotten during restart cleanup;
unexpected errors remain visible and retain the IDs for retry. IDs over 48 hours
old are discarded because Telegram cannot normally delete those messages.
Messages sent before journaling was enabled cannot be discovered automatically.
This cleanup concerns bot UI; it does not clear user-message history or persist
the session's application state.

### Updating other sessions

To push a change to other users (chat rooms, shared boards), call
`viewRefreshQueue.Request(viewModel)` from any handler. Inject the `[Managed]`
`ViewRefreshQueue` into a service. Never await another session's render inside
your own handler, because session locks would deadlock. The queue is started by
`UglyTgApp.Load`. Four workers coalesce duplicate requests over a short 100 ms
window and render each page under its session lock if the page is still visible.
It logs and
skips failures, and stops on shutdown. Offline tests call
`await queue.ProcessPendingAsync()` to drain it deterministically. Unsubscribe
pages in `UnloadAsync` so popped pages are not kept alive.

Pass a shutdown token to `UglyTgApp.Start(token, typeof(MainPageViewModel), shutdown.Token)`.
Keep the host alive until shutdown, as shown in `UglyDemo/Program.cs`.
Incoming user-message deletion remains controlled by
`RemoveUserMessagesPolicyEnabled` (default `true`); group deletion may require
additional bot permissions. Set it to `false` to retain user messages.
An application host can set `RemoveUserMessagesPolicyOverride` to `true` or
`false` to enforce a consistent policy across all states, including shared
dialogs. Its default `null` honors each state's setting. Deletion happens after
successful dispatch, so validation feedback can render before input is removed.

## Verification

With the .NET 6 SDK installed, from this framework directory:

```bash
dotnet build TgUI.sln
dotnet test Tests/Tests.csproj
```

Tests use a simulated Telegram HTTP transport and need no bot token. They cover
session isolation, update serialization, awaited failures, cancellation, rendering,
navigation and callback validation. The original DI tests also run.



