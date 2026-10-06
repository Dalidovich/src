using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using TinyWatcher.App;
using TinyWatcher.Configuration;

namespace TinyWatcher.Recording;

public sealed class RecordingSession : IDisposable
{
    private static readonly TimeSpan WatcherJoinTimeout = TimeSpan.FromSeconds(2);

    private readonly Process process;
    private readonly ProcessJob job = new();
    private readonly Thread watcher;
    private readonly Action<RecordingSession, RecordingResult> completed;

    private RecordingSession(Process process, string filePath, Action<RecordingSession, RecordingResult> completed)
    {
        this.process = process;
        this.completed = completed;
        FilePath = filePath;
        watcher = new Thread(Watch) { IsBackground = true, Name = "FfmpegWatcher" };
    }

    public string FilePath { get; }

    public static RecordingSession Start(
        Rectangle area,
        AppSettings settings,
        string filePath,
        Action<RecordingSession, RecordingResult> completed)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = PortablePaths.FfmpegExecutable,
            WorkingDirectory = PortablePaths.Root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["__COMPAT_LAYER"] = "HighDpiAware";
        foreach (var argument in BuildArguments(area, settings, filePath))
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            throw new RecordingStartException(ex.Message);
        }

        var session = new RecordingSession(process, filePath, completed);
        session.job.Assign(process);
        session.watcher.Start();
        return session;
    }

    public void RequestStop()
    {
        try
        {
            process.StandardInput.Write('q');
            process.StandardInput.Flush();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
        }
    }

    public void Kill()
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
        }
    }

    public bool WaitForExit(TimeSpan timeout) => watcher.Join(timeout);

    public void Dispose()
    {
        watcher.Join(WatcherJoinTimeout);
        process.Dispose();
        job.Dispose();
    }

    private static IEnumerable<string> BuildArguments(Rectangle area, AppSettings settings, string filePath)
    {
        string[] capture =
        [
            "-hide_banner", "-nostats", "-loglevel", "error", "-y",
            "-f", "gdigrab",
            "-framerate", Number(settings.Fps),
            "-draw_mouse", settings.CaptureCursor ? "1" : "0",
            "-offset_x", Number(area.X),
            "-offset_y", Number(area.Y),
            "-video_size", $"{Number(area.Width)}x{Number(area.Height)}",
            "-i", "desktop"
        ];
        string[] limit = settings.MaxRecordingSeconds > 0 ? ["-t", Number(settings.MaxRecordingSeconds)] : [];
        string[] encode =
        [
            "-r", Number(settings.Fps),
            "-fps_mode", "cfr",
            "-c:v", "libx264",
            "-preset", "veryfast",
            "-crf", Number(settings.Crf),
            "-pix_fmt", "yuv420p",
            "-an",
            filePath
        ];
        return [.. capture, .. limit, .. encode];
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private void Watch()
    {
        RecordingResult result;
        try
        {
            var errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();
            result = new RecordingResult(process.ExitCode, errorOutput.Trim());
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            result = new RecordingResult(-1, ex.Message);
        }

        completed(this, result);
    }
}
