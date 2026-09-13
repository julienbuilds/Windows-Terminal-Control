namespace Wctl.Cli;

/// <summary>All commands, looked up by name or alias (case insensitive).</summary>
public sealed class CommandTable
{
    private readonly Dictionary<string, CommandSpec> byName = new(StringComparer.OrdinalIgnoreCase);

    public CommandTable(IEnumerable<CommandSpec> commands)
    {
        All = commands.ToList();
        foreach (var command in All)
        {
            Register(command.Name, command);
            foreach (var alias in command.Aliases)
            {
                Register(alias, command);
            }
        }
    }

    /// <summary>Commands in registration order. Groups appear in the order their first command was registered.</summary>
    public IReadOnlyList<CommandSpec> All { get; }

    public IEnumerable<IGrouping<string, CommandSpec>> Groups => All.GroupBy(c => c.Group);

    public CommandSpec? Find(string nameOrAlias) => byName.GetValueOrDefault(nameOrAlias);

    private void Register(string word, CommandSpec command)
    {
        if (!byName.TryAdd(word, command))
        {
            throw new InvalidOperationException(
                $"'{word}' is used by both '{byName[word].Name}' and '{command.Name}'. Every name and alias must be unique.");
        }
    }
}
