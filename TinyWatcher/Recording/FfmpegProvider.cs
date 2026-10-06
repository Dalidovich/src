using System.ComponentModel;
using NReco.VideoConverter;
using TinyWatcher.App;

namespace TinyWatcher.Recording;

public static class FfmpegProvider
{
    private const string SilentProbeArguments = "-hide_banner -loglevel quiet -f lavfi -i color=s=16x16:d=0.1 -f null -";

    public static void EnsureAvailable()
    {
        if (File.Exists(PortablePaths.FfmpegExecutable))
        {
            return;
        }

        var reason = "the file was not created";
        try
        {
            var converter = new FFMpegConverter { FFMpegToolPath = PortablePaths.Root };
            converter.Invoke(SilentProbeArguments);
        }
        catch (Exception ex) when (ex is FFMpegException or IOException or UnauthorizedAccessException or Win32Exception)
        {
            reason = ex.Message;
        }

        if (!File.Exists(PortablePaths.FfmpegExecutable))
        {
            throw new StartupException($"ffmpeg.exe could not be extracted to {PortablePaths.Root}: {reason}");
        }
    }
}
