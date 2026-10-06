namespace TinyWatcher.Overlay;

internal sealed class RegionFrame : IDisposable
{
    public static readonly Color CountdownColor = Color.FromArgb(0xFD, 0xD8, 0x35);
    public static readonly Color RecordingColor = Color.FromArgb(0xE5, 0x39, 0x35);

    private const int Thickness = 2;

    private readonly List<FrameSide> sides = [];

    public RegionFrame(Rectangle area, Color color)
    {
        var monitor = Screen.FromRectangle(area).Bounds;
        var outer = Rectangle.Inflate(area, Thickness, Thickness);

        AddHorizontal(new Rectangle(outer.Left, outer.Top, outer.Width, Thickness), monitor, color);
        AddHorizontal(new Rectangle(outer.Left, area.Bottom, outer.Width, Thickness), monitor, color);
        AddVertical(new Rectangle(outer.Left, area.Top, Thickness, area.Height), monitor, color);
        AddVertical(new Rectangle(area.Right, area.Top, Thickness, area.Height), monitor, color);

        foreach (var side in sides)
        {
            side.Show();
        }
    }

    public void SetColor(Color color)
    {
        foreach (var side in sides)
        {
            side.BackColor = color;
        }
    }

    public void Dispose()
    {
        foreach (var side in sides)
        {
            side.Close();
            side.Dispose();
        }

        sides.Clear();
    }

    private void AddHorizontal(Rectangle side, Rectangle monitor, Color color)
    {
        if (side.Top >= monitor.Top && side.Bottom <= monitor.Bottom)
        {
            sides.Add(new FrameSide(Rectangle.Intersect(side, monitor), color));
        }
    }

    private void AddVertical(Rectangle side, Rectangle monitor, Color color)
    {
        if (side.Left >= monitor.Left && side.Right <= monitor.Right)
        {
            sides.Add(new FrameSide(side, color));
        }
    }
}
