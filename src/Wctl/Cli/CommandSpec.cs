namespace Wctl.Cli;

/// <summary>
/// Everything there is to know about one command. This is the single source of truth:
/// the router uses it to dispatch, "w help", COMMANDS.md and tab completion are all rendered from it.
/// </summary>
public sealed record CommandSpec
{
    /// <summary>The long name, for example "volume".</summary>
    public required string Name { get; init; }

    /// <summary>Short forms, for example "vol" and "v".</summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>Section in the help output. See <see cref="Groups"/>.</summary>
    public required string Group { get; init; }

    /// <summary>One line, no period, for the command list.</summary>
    public required string Summary { get; init; }

    /// <summary>Arguments without the leading "w ", for example "vol [n | +n | -n | mute]".</summary>
    public required string Usage { get; init; }

    /// <summary>Optional longer text shown by "w help &lt;command&gt;".</summary>
    public string? Details { get; init; }

    /// <summary>How many words the command accepts after its name. More than that is an error, so typos never pass silently.</summary>
    public int MaxArgs { get; init; } = int.MaxValue;

    /// <summary>What tab completion offers after this command. Candidates are filtered by what the user typed.</summary>
    internal Completer? Complete { get; init; }

    public required Func<Invocation, int> Run { get; init; }
}

/// <summary>Returns everything that could follow; the caller narrows it down to what the user has typed.</summary>
internal delegate IEnumerable<Candidate> Completer(CompletionContext context);

/// <summary>Help sections, in display order.</summary>
public static class Groups
{
    public const string Apps = "Apps";
    public const string Files = "Files";
    public const string Windows = "Windows";
    public const string Audio = "Audio";
    public const string Media = "Media";
    public const string Display = "Display";
    public const string System = "System";
    public const string Scenes = "Scenes";
    public const string Help = "Help";
}
