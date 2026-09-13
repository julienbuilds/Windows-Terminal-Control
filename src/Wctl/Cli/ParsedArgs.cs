namespace Wctl.Cli;

/// <summary>
/// The command line split into positional words and the few global flags.
/// Only the known global flags are taken out. "-5", "+5" and unknown "--flags" stay values, because "w vol -5"
/// and "w open code --new-window" must work.
/// </summary>
public sealed record ParsedArgs(
    IReadOnlyList<string> Positionals,
    bool Json,
    bool Help,
    bool Version,
    bool Debug,
    bool Markdown)
{
    public static ParsedArgs Parse(string[] argv)
    {
        var positionals = new List<string>(argv.Length);
        bool json = false, help = false, version = false, debug = false, markdown = false;

        foreach (var arg in argv)
        {
            switch (arg)
            {
                case "--json":
                    json = true;
                    break;
                case "--help" or "-h" or "-?":
                    help = true;
                    break;
                case "--version" or "-V":
                    version = true;
                    break;
                case "--debug":
                    debug = true;
                    break;
                case "--markdown":
                    markdown = true;
                    break;
                default:
                    // Anything else is a value for the command, including other --flags, so 'w open code --new-window' works.
                    // Commands check their own arguments and reject what they do not understand.
                    positionals.Add(arg);
                    break;
            }
        }

        return new ParsedArgs(positionals, json, help, version, debug, markdown);
    }
}
