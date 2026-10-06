namespace TinyWatcher.Configuration;

public sealed class AppSettings
{
    public const string DiscardOnExit = "Discard";
    public const string SaveOnExit = "Save";

    public string Hotkey { get; set; } = "Win+Shift+R";

    public string RecordingsFolder { get; set; } = string.Empty;

    public int Fps { get; set; } = 30;

    public int Crf { get; set; } = 18;

    public bool CaptureCursor { get; set; } = true;

    public int StartDelaySeconds { get; set; }

    public int MaxRecordingSeconds { get; set; }

    public string OnExitWhileRecording { get; set; } = DiscardOnExit;

    public bool SaveRecordingOnExit => string.Equals(OnExitWhileRecording, SaveOnExit, StringComparison.OrdinalIgnoreCase);
}
