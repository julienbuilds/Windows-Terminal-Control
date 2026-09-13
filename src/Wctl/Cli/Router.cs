namespace Wctl.Cli;

/// <summary>Picks the command for a parsed command line and runs it.</summary>
public static class Router
{
    public static int Dispatch(ParsedArgs parsed, CommandTable table, IOutput output, TextWriter stdout)
    {
        if (parsed.Version)
        {
            output.State("wctl", VersionInfo.Current);
            return ExitCodes.Ok;
        }

        if (parsed.Positionals.Count == 0)
        {
            return Help.Overview(table, output);
        }

        var word = parsed.Positionals[0];
        IReadOnlyList<string> rest = parsed.Positionals.Skip(1).ToList();

        var command = table.Find(word);
        if (command is null)
        {
            // "w spotify" means "w open spotify". The open command decides whether the word is something it can open.
            command = table.Find("open")
                ?? throw new WctlException($"Unknown command '{word}'. Run 'w help' to see all commands.");
            rest = parsed.Positionals;
        }

        if (parsed.Help)
        {
            return Help.Command(command, output);
        }

        return command.Run(new Invocation(rest, parsed, output, stdout, table));
    }
}
