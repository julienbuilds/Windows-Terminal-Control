using Spectre.Console;
using Wctl.Commands;
using Wctl.Platform;

namespace Wctl.Cli;

/// <summary>Entry point logic, separated from Program.cs so tests can run the whole tool in process.</summary>
public static class App
{
    public static int Run(string[] argv, CommandTable table, Services services, IAnsiConsole console, IAnsiConsole errorConsole, TextWriter stdout)
    {
        // Completion is handled before the global flags are parsed, because the words being completed may themselves
        // look like flags: "w apps --json <tab>" must still complete, not print JSON.
        if (argv.Length > 0 && argv[0].Equals(CompleteCommand.Spec.Name, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return Completion.Run(argv[1..], table, services, stdout);
            }
            catch (WctlException e)
            {
                new ConsoleOutput(console, errorConsole).Fail(e.Message);
                return e.ExitCode;
            }
        }

        ParsedArgs parsed;
        try
        {
            parsed = ParsedArgs.Parse(argv);
        }
        catch (WctlException e)
        {
            new ConsoleOutput(console, errorConsole).Fail(e.Message);
            return e.ExitCode;
        }

        IOutput output = parsed.Json ? new JsonOutput(stdout) : new ConsoleOutput(console, errorConsole);

        try
        {
            var code = Router.Dispatch(parsed, table, services, output, stdout);
            output.Flush();
            return code;
        }
        catch (WctlException e)
        {
            output.Fail(e.Message);
            output.Flush();
            return e.ExitCode;
        }
        catch (Exception e) when (!parsed.Debug)
        {
            // With --debug the exception escapes and .NET prints the full stack trace.
            output.Fail($"Unexpected {e.GetType().Name}: {e.Message} (run again with --debug for the stack trace)");
            output.Flush();
            return ExitCodes.Unexpected;
        }
    }
}
