using System.Drawing.Text;
using System.Globalization;

namespace TinyWatcher.Overlay;

internal sealed class CountdownWindow : PassThroughForm
{
    private const int MaxBoxSize = 160;
    private const double BoxOpacity = 0.85;
    private const float DigitHeightRatio = 0.6f;

    private readonly Font font;
    private int number;

    public CountdownWindow(Rectangle area)
        : base(CenteredBox(area), BoxOpacity)
    {
        BackColor = Color.FromArgb(0x20, 0x20, 0x20);
        DoubleBuffered = true;
        font = new Font("Segoe UI", Math.Max(8f, Height * DigitHeightRatio), FontStyle.Bold, GraphicsUnit.Pixel);
    }

    public void ShowNumber(int value)
    {
        number = value;
        if (!Visible)
        {
            Show();
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(number.ToString(CultureInfo.InvariantCulture), font, Brushes.White, ClientRectangle, format);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            font.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Rectangle CenteredBox(Rectangle area)
    {
        var size = Math.Min(MaxBoxSize, Math.Min(area.Width, area.Height));
        return new Rectangle(area.Left + (area.Width - size) / 2, area.Top + (area.Height - size) / 2, size, size);
    }
}
