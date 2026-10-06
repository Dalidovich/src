using System.Runtime.InteropServices;
using TinyWatcher.Interop;

namespace TinyWatcher.Hotkeys;

public sealed class HotkeyListener : NativeWindow, IDisposable
{
    private const int HotkeyId = 1;
    private const int ErrorHotkeyAlreadyRegistered = 1409;

    private bool registered;

    private HotkeyListener()
    {
    }

    public event Action? Pressed;

    public static HotkeyListener? Register(HotkeyDefinition hotkey, out string? error)
    {
        var listener = new HotkeyListener();
        listener.CreateHandle(new CreateParams { Parent = User32.HwndMessage });

        if (User32.RegisterHotKey(listener.Handle, HotkeyId, hotkey.Modifiers | User32.ModNoRepeat, (uint)hotkey.Key))
        {
            listener.registered = true;
            error = null;
            return listener;
        }

        var errorCode = Marshal.GetLastPInvokeError();
        error = errorCode == ErrorHotkeyAlreadyRegistered
            ? "the key combination is already registered by another application"
            : $"Win32 error {errorCode}";
        listener.DestroyHandle();
        return null;
    }

    public void Dispose()
    {
        if (Handle == 0)
        {
            return;
        }

        if (registered)
        {
            User32.UnregisterHotKey(Handle, HotkeyId);
            registered = false;
        }

        DestroyHandle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == User32.WmHotkey && m.WParam == HotkeyId)
        {
            Pressed?.Invoke();
            return;
        }

        base.WndProc(ref m);
    }
}
