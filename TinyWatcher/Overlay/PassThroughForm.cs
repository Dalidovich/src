using TinyWatcher.Interop;

namespace TinyWatcher.Overlay;

internal abstract class PassThroughForm : ScreenSpaceForm
{
    protected PassThroughForm(Rectangle screenBounds, double opacity)
        : base(screenBounds)
    {
        Opacity = opacity;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= User32.WsExTransparent | User32.WsExNoActivate;
            return parameters;
        }
    }
}
