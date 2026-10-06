using TinyWatcher.App;

namespace TinyWatcher.Configuration;

public static class SettingsValidator
{
    private const int MaxCrf = 51;

    public static void Validate(AppSettings settings)
    {
        if (settings.RecordingsFolder is null)
        {
            settings.RecordingsFolder = string.Empty;
        }

        if (settings.RecordingsFolder.Length > 0 && !Path.IsPathFullyQualified(settings.RecordingsFolder))
        {
            throw Invalid(nameof(AppSettings.RecordingsFolder), "must be empty or an absolute path", settings.RecordingsFolder);
        }

        if (settings.Fps <= 0)
        {
            throw Invalid(nameof(AppSettings.Fps), "must be greater than 0", settings.Fps);
        }

        if (settings.Crf is < 0 or > MaxCrf)
        {
            throw Invalid(nameof(AppSettings.Crf), $"must be between 0 and {MaxCrf}", settings.Crf);
        }

        if (settings.StartDelaySeconds < 0)
        {
            throw Invalid(nameof(AppSettings.StartDelaySeconds), "must be 0 or greater", settings.StartDelaySeconds);
        }

        if (settings.MaxRecordingSeconds < 0)
        {
            throw Invalid(nameof(AppSettings.MaxRecordingSeconds), "must be 0 or greater", settings.MaxRecordingSeconds);
        }

        if (settings.OnExitWhileRecording is not (AppSettings.DiscardOnExit or AppSettings.SaveOnExit))
        {
            throw Invalid(
                nameof(AppSettings.OnExitWhileRecording),
                $"must be \"{AppSettings.DiscardOnExit}\" or \"{AppSettings.SaveOnExit}\"",
                settings.OnExitWhileRecording);
        }
    }

    private static StartupException Invalid(string field, string rule, object? actual) =>
        new($"settings.json: {field} {rule}, got \"{actual}\".");
}
