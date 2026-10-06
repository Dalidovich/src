using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TinyWatcher.Overlay;

namespace TinyWatcher.Selection;

internal sealed class SelectionOverlay : ScreenSpaceForm
{
    private const int MinAreaSize = 16;
    private const int DimAlpha = 140;
    private const int LabelGap = 6;
    private const int LabelPadding = 4;
    private const float LabelFontSize = 14f;
    private const float DefaultDpi = 96f;

    private readonly Rectangle screenBounds;
    private readonly Bitmap snapshot;
    private readonly Bitmap dimmed;
    private readonly Font labelFont;
    private Point anchor;
    private Rectangle selection;
    private bool dragging;

    public SelectionOverlay(Screen screen)
        : base(screen.Bounds)
    {
        screenBounds = screen.Bounds;
        snapshot = CaptureScreen(screenBounds);
        dimmed = Dim(snapshot);
        labelFont = new Font("Segoe UI", LabelFontSize * DeviceDpi / DefaultDpi, FontStyle.Regular, GraphicsUnit.Pixel);
        DoubleBuffered = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
    }

    public event Action<Rectangle?>? Finished;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var full = new Rectangle(Point.Empty, snapshot.Size);
        graphics.DrawImage(dimmed, full, full, GraphicsUnit.Pixel);
        if (!dragging || selection.IsEmpty)
        {
            return;
        }

        graphics.DrawImage(snapshot, selection, selection, GraphicsUnit.Pixel);
        graphics.PixelOffsetMode = PixelOffsetMode.Default;
        graphics.DrawRectangle(Pens.White, selection.X, selection.Y, selection.Width - 1, selection.Height - 1);
        DrawSizeLabel(graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right)
        {
            Finished?.Invoke(null);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            anchor = ClampToScreen(e.Location);
            dragging = true;
            UpdateSelection(e.Location);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging)
        {
            UpdateSelection(e.Location);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !dragging)
        {
            return;
        }

        UpdateSelection(e.Location);
        dragging = false;
        Finished?.Invoke(ToRecordingArea(selection));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            Finished?.Invoke(null);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            snapshot.Dispose();
            dimmed.Dispose();
            labelFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Bitmap CaptureScreen(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Bitmap Dim(Bitmap source)
    {
        var bitmap = new Bitmap(source);
        using var graphics = Graphics.FromImage(bitmap);
        using var shade = new SolidBrush(Color.FromArgb(DimAlpha, Color.Black));
        graphics.FillRectangle(shade, 0, 0, bitmap.Width, bitmap.Height);
        return bitmap;
    }

    private Rectangle? ToRecordingArea(Rectangle selected)
    {
        if (selected.Width < MinAreaSize || selected.Height < MinAreaSize)
        {
            return null;
        }

        return new Rectangle(
            screenBounds.X + selected.X,
            screenBounds.Y + selected.Y,
            selected.Width & ~1,
            selected.Height & ~1);
    }

    private Point ClampToScreen(Point point) => new(
        Math.Clamp(point.X, 0, screenBounds.Width - 1),
        Math.Clamp(point.Y, 0, screenBounds.Height - 1));

    private void UpdateSelection(Point cursor)
    {
        var current = ClampToScreen(cursor);
        selection = Rectangle.FromLTRB(
            Math.Min(anchor.X, current.X),
            Math.Min(anchor.Y, current.Y),
            Math.Max(anchor.X, current.X) + 1,
            Math.Max(anchor.Y, current.Y) + 1);
        Invalidate();
    }

    private void DrawSizeLabel(Graphics graphics)
    {
        var text = $"{selection.Width}×{selection.Height}";
        var textSize = Size.Ceiling(graphics.MeasureString(text, labelFont));
        var box = new Rectangle(0, 0, textSize.Width + LabelPadding * 2, textSize.Height + LabelPadding * 2);
        box.X = Math.Clamp(selection.Right - box.Width, 0, Math.Max(0, screenBounds.Width - box.Width));
        box.Y = selection.Bottom + LabelGap;
        if (box.Bottom > screenBounds.Height)
        {
            box.Y = Math.Max(0, selection.Top - LabelGap - box.Height);
        }

        using var background = new SolidBrush(Color.FromArgb(220, 0x20, 0x20, 0x20));
        graphics.FillRectangle(background, box);
        graphics.DrawString(text, labelFont, Brushes.White, box.X + LabelPadding, box.Y + LabelPadding);
    }
}
