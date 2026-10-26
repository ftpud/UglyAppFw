using UglyDemo.ViewModels;
using UglyTgApplication;

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};

UglyTgApp.Start(File.ReadAllText("token.txt").Trim(), typeof(MainPageViewModel), shutdown.Token);
Console.WriteLine("ToDo bot running. Press Ctrl+C to stop.");
try
{
    await Task.Delay(Timeout.Infinite, shutdown.Token);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
    // Normal shutdown.
}
