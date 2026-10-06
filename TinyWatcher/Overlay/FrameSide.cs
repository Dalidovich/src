namespace TinyWatcher.Overlay;

internal sealed class FrameSide : PassThroughForm
{
    private const double NearlyOpaque = 0.99;

    public FrameSide(Rectangle screenBounds, Color color)
        : base(screenBounds, NearlyOpaque)
    {
        BackColor = color;
    }
}
