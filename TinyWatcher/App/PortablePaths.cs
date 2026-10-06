namespace TinyWatcher.App;

public static class PortablePaths
{
    public static string ExecutablePath { get; } = ResolveExecutablePath();

    public static string Root { get; } = Path.GetDirectoryName(ExecutablePath) ?? Directory.GetCurrentDirectory();

    public static string SettingsFile => Path.Combine(Root, "settings.json");

    public static string FfmpegExecutable => Path.Combine(Root, "ffmpeg.exe");

    public static string DefaultRecordingsDirectory => Path.Combine(Root, "recordings");

    public static string ResolveRecordingsDirectory(string configuredFolder) =>
        string.IsNullOrWhiteSpace(configuredFolder) ? DefaultRecordingsDirectory : configuredFolder;

    private static string ResolveExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            return Path.GetFullPath(processPath);
        }

        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var modulePath = process.MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(modulePath))
        {
            return Path.GetFullPath(modulePath);
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "TinyWatcher.exe");
    }
}
