using Wctl.Cli;
using Wctl.Scenes;

namespace Wctl.Commands.Scenes;

/// <summary>Runs the steps of a scene in order, reports each one, keeps going when one fails.</summary>
internal static class SceneRunner
{
    /// <summary>How long a window step keeps looking for a window of an app the scene opened.</summary>
    private static readonly TimeSpan WindowWait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryPause = TimeSpan.FromMilliseconds(500);

    public static int Run(Scene scene, Invocation inv)
    {
        inv.Output.State("Scene", scene.Name);

        var failed = 0;
        DateTimeOffset? windowDeadline = null;
        foreach (var step in scene.Steps)
        {
            var (ok, result) = RunStep(step, inv, windowDeadline);
            inv.Output.StepResult(step, ok, result);
            if (!ok)
            {
                failed++;
            }

            if (ok && IsOpen(step, inv.Table))
            {
                // The app gets one time budget to show its window. Later window steps share it instead of each waiting again.
                windowDeadline = inv.Services.Clock.Now + WindowWait;
            }
        }

        inv.Output.Detail("Step count", scene.Steps.Count);
        inv.Output.Detail("Failed", failed);
        if (failed > 0)
        {
            throw new WctlException($"{failed} of {scene.Steps.Count} steps failed.");
        }

        inv.Output.Message($"{scene.Steps.Count} steps, all done.");
        return ExitCodes.Ok;
    }

    /// <summary>Runs one step with its output captured. Never throws for a failing step; the failure is the result.</summary>
    public static (bool Ok, string Result) RunStep(string step, Invocation inv, DateTimeOffset? windowDeadline)
    {
        string[] argv;
        try
        {
            argv = StepLine.Split(step);
        }
        catch (WctlException e)
        {
            return (false, e.Message);
        }

        if (argv.Length == 0)
        {
            return (false, "empty step");
        }

        var sceneCommand = inv.Table.Find("scene");
        if (sceneCommand is not null && inv.Table.Find(argv[0]) == sceneCommand)
        {
            return (false, "a scene cannot run another scene");
        }

        var clock = inv.Services.Clock;
        while (true)
        {
            var capture = new StepCapture();
            try
            {
                var code = Router.Dispatch(ParsedArgs.Parse(argv), inv.Table, inv.Services, capture, TextWriter.Null, sceneFallback: false);
                return (code == ExitCodes.Ok, capture.Text);
            }
            catch (WindowNotFoundException) when (windowDeadline is not null && clock.Now < windowDeadline)
            {
                // The app is probably still starting. Give it a moment and look again.
                clock.Delay(RetryPause, CancellationToken.None);
            }
            catch (WctlException e)
            {
                return (false, e.Message);
            }
        }
    }

    /// <summary>True when the step starts an app: "open ...", its alias, or a bare app name.</summary>
    private static bool IsOpen(string step, CommandTable table)
    {
        var first = StepLine.Split(step)[0];
        var command = table.Find(first);
        return command is null || command == table.Find("open");
    }
}
