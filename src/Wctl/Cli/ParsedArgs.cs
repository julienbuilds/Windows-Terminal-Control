namespace Wctl.Cli;

/// <summary>
/// The command line split into positional words and the few global flags.
/// Only words starting with "--" (plus -h and -V) are flags. "-5" and "+5" are values, because "w vol -5" must work.
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
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new WctlException($"Unknown option '{arg}'. Known options: --json, --help, --version, --debug.");
                    }

                    positionals.Add(arg);
                    break;
            }
        }

        return new ParsedArgs(positionals, json, help, version, debug, markdown);
    }
}
