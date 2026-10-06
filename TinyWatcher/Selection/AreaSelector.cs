namespace TinyWatcher.Selection;

internal sealed class AreaSelector : IDisposable
{
    private readonly List<SelectionOverlay> overlays = [];
    private readonly Action<Rectangle?> finished;
    private bool closed;

    public AreaSelector(Action<Rectangle?> finished)
    {
        this.finished = finished;
        foreach (var screen in Screen.AllScreens)
        {
            var overlay = new SelectionOverlay(screen);
            overlay.Finished += OnFinished;
            overlays.Add(overlay);
        }

        foreach (var overlay in overlays)
        {
            overlay.Show();
        }

        var active = overlays.FirstOrDefault(overlay => overlay.Bounds.Contains(Cursor.Position)) ?? overlays.FirstOrDefault();
        active?.Activate();
    }

    public void Dispose()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        foreach (var overlay in overlays)
        {
            overlay.Finished -= OnFinished;
            overlay.Close();
            overlay.Dispose();
        }

        overlays.Clear();
    }

    private void OnFinished(Rectangle? area)
    {
        if (closed)
        {
            return;
        }

        Dispose();
        finished(area);
    }
}
