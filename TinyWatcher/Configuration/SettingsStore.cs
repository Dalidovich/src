using System.Text.Encodings.Web;
using System.Text.Json;
using TinyWatcher.App;

namespace TinyWatcher.Configuration;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IgnoreReadOnlyProperties = true
    };

    public static AppSettings Load()
    {
        var path = PortablePaths.SettingsFile;
        if (!File.Exists(path))
        {
            var created = new AppSettings();
            Save(created, "settings.json was not created");
            return created;
        }

        if (TryRead(path, out var settings))
        {
            return settings;
        }

        ConsoleLog.Warning("settings.json is invalid, using defaults.");
        var defaults = new AppSettings();
        Save(defaults, "settings.json was not rewritten");
        return defaults;
    }

    private static bool TryRead(string path, out AppSettings settings)
    {
        settings = new AppSettings();
        try
        {
            var parsed = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), SerializerOptions);
            if (parsed is null)
            {
                return false;
            }

            settings = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static void Save(AppSettings settings, string failureMessage)
    {
        try
        {
            File.WriteAllText(PortablePaths.SettingsFile, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConsoleLog.Warning($"Application folder is not writable, {failureMessage}.");
        }
    }
}
