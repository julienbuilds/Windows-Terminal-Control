using Spectre.Console;

namespace Wctl.Cli;

/// <summary>Entry point logic, separated from Program.cs so tests can run the whole tool in process.</summary>
public static class App
{
    public static int Run(string[] argv, CommandTable table, IAnsiConsole console, IAnsiConsole errorConsole, TextWriter stdout)
    {
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
            var code = Router.Dispatch(parsed, table, output, stdout);
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
