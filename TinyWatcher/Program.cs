using TinyWatcher.App;

namespace TinyWatcher;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var app = new TinyWatcherApp();
        return app.Run();
    }
}
