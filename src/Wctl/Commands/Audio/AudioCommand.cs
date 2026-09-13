using System.Globalization;
using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Audio;

public static class AudioCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "audio",
        Aliases = ["a"],
        Group = Groups.Audio,
        Summary = "List audio outputs, or switch to one",
        Usage = "audio [<device>]",
        Details = "Pick a device by its number in the list or by part of its name: 'w audio fiio'. "
            + "The device becomes the default for media, system sounds and calls.",
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var audio = inv.Services.Audio;
        var devices = audio.GetOutputDevices();
        if (devices.Count == 0)
        {
            throw new WctlException("No audio output devices found.");
        }

        if (inv.Args.Count == 0)
        {
            return List(devices, audio.GetDefaultOutputId(), inv.Output);
        }

        var query = string.Join(' ', inv.Args);
        var device = Pick(devices, query);
        audio.SetDefaultOutput(device.Id);
        inv.Output.State("Audio output", device.Name);
        return ExitCodes.Ok;
    }

    private static int List(IReadOnlyList<AudioDevice> devices, string? defaultId, IOutput output)
    {
        var rows = devices.Select((d, i) => new[] { (i + 1).ToString(CultureInfo.InvariantCulture), d.Name }).ToList();
        var current = defaultId is null ? -1 : devices.ToList().FindIndex(d => d.Id == defaultId);
        output.Table("outputs", ["index", "name"], rows, new TableOptions(CurrentRow: current));
        return ExitCodes.Ok;
    }

    private static AudioDevice Pick(IReadOnlyList<AudioDevice> devices, string query)
    {
        if (int.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            if (index < 1 || index > devices.Count)
            {
                throw new WctlException($"There is no audio output number {index}. Run 'w audio' to see the list.");
            }

            return devices[index - 1];
        }

        var matches = devices.Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new WctlException($"No audio output matches '{query}'. Available: {Names(devices)}."),
            _ => throw new WctlException($"More than one audio output matches '{query}': {Names(matches)}. Be more specific or use the number from 'w audio'."),
        };
    }

    private static string Names(IEnumerable<AudioDevice> devices) => string.Join(", ", devices.Select(d => d.Name));
}
