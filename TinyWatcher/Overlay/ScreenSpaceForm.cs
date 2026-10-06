using TinyWatcher.Interop;

namespace TinyWatcher.Overlay;

internal abstract class ScreenSpaceForm : Form
{
    private readonly Rectangle screenBounds;

    protected ScreenSpaceForm(Rectangle screenBounds)
    {
        this.screenBounds = screenBounds;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        MinimumSize = Size.Empty;
        Bounds = screenBounds;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= User32.WsExToolWindow | User32.WsExTopmost;
            return parameters;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Bounds = screenBounds;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg is User32.WmDpiChanged or User32.WmGetDpiScaledSize)
        {
            m.Result = 0;
            return;
        }

        base.WndProc(ref m);
    }
}
