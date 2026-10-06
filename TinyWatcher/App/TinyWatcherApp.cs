using TinyWatcher.Configuration;
using TinyWatcher.Hotkeys;
using TinyWatcher.Recording;
using TinyWatcher.Taskbar;

namespace TinyWatcher.App;

public sealed class TinyWatcherApp
{
    public int Run()
    {
        try
        {
            RunCore();
            return 0;
        }
        catch (StartupException ex)
        {
            ConsoleLog.Error(ex.Message);
            WaitForKey();
            return 1;
        }
    }

    private static void RunCore()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        var settings = SettingsStore.Load();
        SettingsValidator.Validate(settings);
        if (!HotkeyParser.TryParse(settings.Hotkey, out var hotkey))
        {
            throw new StartupException($"settings.json: Hotkey \"{settings.Hotkey}\" is not a valid key combination.");
        }

        FfmpegProvider.EnsureAvailable();

        var ui = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(ui);

        using var listener = HotkeyListener.Register(hotkey, out var hotkeyError)
            ?? throw new StartupException($"Hotkey {hotkey.DisplayName} could not be registered: {hotkeyError}.");
        using var shutdown = new ShutdownSignal(() => ui.Post(_ => Application.ExitThread(), null));
        try
        {
            using var badge = TaskbarBadge.Attach();
            using var controller = new RecordingController(settings, badge, ui);
            Application.ThreadException += (_, e) => controller.Recover(e.Exception);
            listener.Pressed += controller.OnHotkey;

            ConsoleLog.Info($"TinyWatcher is ready. Press {hotkey.DisplayName} to select an area, press it again to stop recording.");
            Application.Run();

            listener.Pressed -= controller.OnHotkey;
            controller.Shutdown();
        }
        finally
        {
            listener.Dispose();
            shutdown.MarkCleanupCompleted();
        }
    }

    private static void WaitForKey()
    {
        ConsoleLog.Info("Press any key to exit");
        try
        {
            Console.ReadKey(intercept: true);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
