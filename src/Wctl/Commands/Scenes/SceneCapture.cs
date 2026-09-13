using Wctl.Commands.Display;
using Wctl.Platform;

namespace Wctl.Commands.Scenes;

/// <summary>Turns the current state of the PC into scene steps: sound, displays, then open apps and where their windows are.</summary>
internal static class SceneCapture
{
    public static List<string> Capture(Wctl.Cli.Invocation inv)
    {
        var services = inv.Services;
        var steps = new List<string>();

        var audio = services.Audio;
        steps.Add($"vol {audio.GetVolume()}");
        if (audio.GetMuted())
        {
            steps.Add("mute on");
        }

        steps.Add(audio.GetMicMuted() ? "mic mute" : "mic unmute");

        var defaultId = audio.GetDefaultOutputId();
        var output = audio.GetOutputDevices().FirstOrDefault(d => d.Id == defaultId);
        if (output is not null)
        {
            steps.Add($"audio {Quote(output.Name)}");
        }

        var monitors = services.Windows.Monitors();
        var hdr = services.Display.GetHdr().Where(d => d.Supported).ToList();
        if (hdr.Count == 1)
        {
            steps.Add($"hdr {OnOff(hdr[0].Enabled)}");
        }
        else
        {
            steps.AddRange(hdr.Select(d => $"hdr {OnOff(d.Enabled)} monitor {MonitorNames.Label(monitors, d.Device)}"));
        }

        steps.AddRange(services.Display.GetBrightness()
            .Where(m => m.Supported)
            .Select(m => $"brightness {m.Percent} monitor {MonitorNames.Label(monitors, m.Device)}"));

        // One entry per app, using its frontmost window. Open everything first, give the apps a moment, then place the windows.
        var apps = services.Windows.List()
            .GroupBy(w => w.App, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (apps.Count > 0)
        {
            var installed = inv.AppCatalog.Apps;
            steps.AddRange(apps.Select(w => $"open {Quote(AppNames.ForOpen(w, installed))}"));
            steps.Add("wait 2s");
            foreach (var window in apps)
            {
                var app = Quote(window.App);
                if (window.Minimized)
                {
                    steps.Add($"min {app}");
                }
                else if (window.Maximized)
                {
                    if (window.Monitor > 0)
                    {
                        steps.Add($"move {app} {window.Monitor}");
                    }

                    steps.Add($"max {app}");
                }
                else
                {
                    var frame = services.Windows.GetFrame(window.Handle);
                    steps.Add($"move {app} {frame.Left} {frame.Top} {frame.Width} {frame.Height}");
                }

                if (window.TopMost)
                {
                    steps.Add($"top {app} on");
                }
            }
        }

        return steps;
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    private static string Quote(string word) => word.Contains(' ', StringComparison.Ordinal) ? $"\"{word}\"" : word;
}
