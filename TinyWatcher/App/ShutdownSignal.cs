using TinyWatcher.Interop;

namespace TinyWatcher.App;

public sealed class ShutdownSignal : IDisposable
{
    private static readonly TimeSpan CloseCleanupTimeout = TimeSpan.FromSeconds(4);

    private readonly Action requested;
    private readonly ManualResetEventSlim cleanupCompleted = new(false);
    private readonly Kernel32.ConsoleCtrlHandler handler;

    public ShutdownSignal(Action requested)
    {
        this.requested = requested;
        handler = OnConsoleControl;
        Kernel32.SetConsoleCtrlHandler(handler, true);
    }

    public void MarkCleanupCompleted() => cleanupCompleted.Set();

    public void Dispose()
    {
        Kernel32.SetConsoleCtrlHandler(handler, false);
        GC.KeepAlive(handler);
    }

    private bool OnConsoleControl(uint controlType)
    {
        requested();

        if (controlType is not (Kernel32.CtrlCEvent or Kernel32.CtrlBreakEvent))
        {
            cleanupCompleted.Wait(CloseCleanupTimeout);
        }

        return true;
    }
}
