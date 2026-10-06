using System.Collections.Specialized;
using System.Runtime.InteropServices;
using TinyWatcher.App;
using TinyWatcher.Configuration;
using TinyWatcher.Overlay;
using TinyWatcher.Selection;
using TinyWatcher.Taskbar;

namespace TinyWatcher.Recording;

public sealed class RecordingController : IDisposable
{
    private static readonly TimeSpan SaveOnExitTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan KillTimeout = TimeSpan.FromSeconds(2);

    private readonly AppSettings settings;
    private readonly TaskbarBadge? badge;
    private readonly SynchronizationContext ui;
    private readonly System.Windows.Forms.Timer countdownTimer = new() { Interval = 1000 };

    private RecorderState state = RecorderState.Idle;
    private AreaSelector? selector;
    private RegionFrame? frame;
    private CountdownWindow? countdown;
    private RecordingSession? session;
    private Rectangle area;
    private int secondsLeft;

    public RecordingController(AppSettings settings, TaskbarBadge? badge, SynchronizationContext ui)
    {
        this.settings = settings;
        this.badge = badge;
        this.ui = ui;
        countdownTimer.Tick += (_, _) => OnCountdownTick();
        badge?.Show(state);
    }

    public void OnHotkey()
    {
        switch (state)
        {
            case RecorderState.Idle:
                BeginSelection();
                break;
            case RecorderState.Countdown:
                CancelCountdown();
                break;
            case RecorderState.Recording:
                StopRecording();
                break;
        }
    }

    public void Recover(Exception exception)
    {
        ConsoleLog.Error($"Unexpected failure: {ConsoleLog.Describe(exception)}");
        CloseWindows();
        if (session is { } failed)
        {
            session = null;
            Discard(failed);
        }

        SetState(RecorderState.Idle);
    }

    public void Shutdown()
    {
        CloseWindows();
        if (session is not { } active)
        {
            return;
        }

        session = null;
        if (!settings.SaveRecordingOnExit)
        {
            Discard(active);
            return;
        }

        if (state == RecorderState.Recording)
        {
            active.RequestStop();
        }

        if (active.WaitForExit(SaveOnExitTimeout))
        {
            ConsoleLog.Info($"Saved: {active.FilePath}");
        }
        else
        {
            active.Kill();
            active.WaitForExit(KillTimeout);
            ConsoleLog.Warning($"ffmpeg did not finish in time, the file may be incomplete: {active.FilePath}");
        }

        active.Dispose();
    }

    public void Dispose()
    {
        countdownTimer.Dispose();
    }

    private static void Discard(RecordingSession discarded)
    {
        discarded.Kill();
        discarded.WaitForExit(KillTimeout);
        discarded.Dispose();
        DeleteFile(discarded.FilePath);
    }

    private static void DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConsoleLog.Warning($"Incomplete file was not deleted: {path}");
        }
    }

    private static bool HasContent(string path)
    {
        var file = new FileInfo(path);
        return file.Exists && file.Length > 0;
    }

    private void BeginSelection()
    {
        selector = new AreaSelector(OnAreaSelected);
        SetState(RecorderState.Selecting);
    }

    private void OnAreaSelected(Rectangle? selected)
    {
        selector = null;
        if (selected is not { } chosen)
        {
            SetState(RecorderState.Idle);
            return;
        }

        area = chosen;
        if (settings.StartDelaySeconds > 0)
        {
            StartCountdown();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartCountdown()
    {
        secondsLeft = settings.StartDelaySeconds;
        frame = new RegionFrame(area, RegionFrame.CountdownColor);
        countdown = new CountdownWindow(area);
        countdown.ShowNumber(secondsLeft);
        countdownTimer.Start();
        SetState(RecorderState.Countdown);
    }

    private void OnCountdownTick()
    {
        secondsLeft--;
        if (secondsLeft > 0)
        {
            countdown?.ShowNumber(secondsLeft);
            return;
        }

        countdownTimer.Stop();
        CloseCountdown();
        StartRecording();
    }

    private void CancelCountdown()
    {
        countdownTimer.Stop();
        CloseCountdown();
        CloseFrame();
        SetState(RecorderState.Idle);
    }

    private void StartRecording()
    {
        var directory = PortablePaths.ResolveRecordingsDirectory(settings.RecordingsFolder);
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"TinyWatcher_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4");

        if (frame is null)
        {
            frame = new RegionFrame(area, RegionFrame.RecordingColor);
        }
        else
        {
            frame.SetColor(RegionFrame.RecordingColor);
        }

        try
        {
            session = RecordingSession.Start(area, settings, filePath, PostCompletion);
        }
        catch (RecordingStartException ex)
        {
            ConsoleLog.Error($"ffmpeg failed to start: {ex.Message}");
            CloseFrame();
            DeleteFile(filePath);
            SetState(RecorderState.Idle);
            return;
        }

        SetState(RecorderState.Recording);
    }

    private void StopRecording()
    {
        CloseFrame();
        SetState(RecorderState.Finalizing);
        session?.RequestStop();
    }

    private void PostCompletion(RecordingSession completed, RecordingResult result) =>
        ui.Post(_ => OnRecordingCompleted(completed, result), null);

    private void OnRecordingCompleted(RecordingSession completed, RecordingResult result)
    {
        if (!ReferenceEquals(completed, session))
        {
            return;
        }

        session = null;
        completed.Dispose();
        CloseFrame();

        if (result.ExitCode != 0 || !HasContent(completed.FilePath))
        {
            var details = result.ErrorOutput.Length > 0 ? result.ErrorOutput : "no output";
            ConsoleLog.Error($"ffmpeg exited with code {result.ExitCode}: {details}");
            DeleteFile(completed.FilePath);
            SetState(RecorderState.Idle);
            return;
        }

        if (state == RecorderState.Recording)
        {
            SetState(RecorderState.Finalizing);
        }

        CopyToClipboard(completed.FilePath);
        ConsoleLog.Info($"Saved: {completed.FilePath}");
        SetState(RecorderState.Idle);
    }

    private void CopyToClipboard(string filePath)
    {
        try
        {
            Clipboard.SetFileDropList(new StringCollection { filePath });
        }
        catch (ExternalException ex)
        {
            ConsoleLog.Warning($"The file was not copied to the clipboard: {ConsoleLog.Describe(ex)}");
        }
    }

    private void CloseWindows()
    {
        countdownTimer.Stop();
        selector?.Dispose();
        selector = null;
        CloseCountdown();
        CloseFrame();
    }

    private void CloseCountdown()
    {
        countdown?.Close();
        countdown?.Dispose();
        countdown = null;
    }

    private void CloseFrame()
    {
        frame?.Dispose();
        frame = null;
    }

    private void SetState(RecorderState next)
    {
        if (state == next)
        {
            return;
        }

        state = next;
        badge?.Show(next);
        ConsoleLog.Event($"State: {next}");
    }
}
