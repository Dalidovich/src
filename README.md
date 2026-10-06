# TinyWatcher

A Windows console app that records a rectangular area of the screen to an MP4 file. Press a global hotkey, drag a rectangle, press the hotkey again — the video is saved and copied to the clipboard. It is meant to be as quick as taking a screenshot with `Win+Shift+S`, with no scenes or sources to set up.

## Usage

1. Run `TinyWatcher.exe`. A console opens and prints the active hotkey.
2. Press `Win+Shift+R`. All monitors dim and show a frozen snapshot.
3. Drag a rectangle with the left mouse button. Its size is shown next to it.
4. Recording starts when you release the button. A red frame marks the area; the frame is not part of the video.
5. Press `Win+Shift+R` again. The file is saved to `recordings/` next to the exe and placed on the clipboard, ready for `Ctrl+V` in Explorer or a messenger.

To cancel a selection, press `Esc`, right-click, or drag an area smaller than 16×16 px. To quit, press `Ctrl+C` or close the console window.

The colored circle on the taskbar button shows the state: green is idle, red is recording, yellow is anything in between.

Limits: no audio, MP4 (H.264) only, and the area cannot span two monitors.

## Settings

`settings.json` is created next to the exe on first run. It is read once at startup, so restart the app after editing it.

| Field | Default | Meaning |
|---|---|---|
| `Hotkey` | `Win+Shift+R` | Start/stop combination, in the form `Mod+Mod+Key` |
| `RecordingsFolder` | `""` | Absolute path for recordings; empty means `recordings/` next to the exe |
| `Fps` | `30` | Frame rate, greater than 0 |
| `Crf` | `18` | x264 quality, 0–51; lower is better quality and a larger file |
| `CaptureCursor` | `true` | Include the mouse cursor in the video |
| `StartDelaySeconds` | `0` | Countdown before recording; `0` starts immediately |
| `MaxRecordingSeconds` | `0` | Automatic stop after this many seconds; `0` means no limit |
| `OnExitWhileRecording` | `Discard` | What happens to a recording in progress on exit: `Discard` or `Save` |

An out-of-range value or a hotkey that is already taken stops the app at startup with an error naming the problem. A file that is not valid JSON is replaced with the defaults.

## Build

Requires the .NET 9 SDK on Windows x64.

```
dotnet publish TinyWatcher/TinyWatcher.csproj -c Release -o publish
```

The output is a self-contained `TinyWatcher.exe` with `ffmpeg.exe` beside it; no .NET installation is needed to run it.

## Portability

The app writes only to its own folder and to `RecordingsFolder` if you set one. It does not touch the registry, `%APPDATA%`, `%LOCALAPPDATA%` or `%TEMP%`, and makes no network requests. Deleting the folder removes it completely.

## ffmpeg

Recording is done by `ffmpeg.exe` (FFmpeg 5.1.2, GPL build from gyan.dev), which comes from the `NReco.VideoConverter` NuGet package. The binary is not stored in this repository. If it is missing at startup, the app extracts it into its own folder.
