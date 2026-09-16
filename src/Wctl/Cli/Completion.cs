using Wctl.Commands.Apps;
using Wctl.Platform;

namespace Wctl.Cli;

/// <summary>Works out what could come next on a half typed command line.</summary>
internal static class Completion
{
    /// <summary>
    /// Marks that a fresh word is being typed, so the candidates are for the next argument rather than the last one.
    /// It exists because PowerShell drops empty arguments on the way to a program, so an empty word cannot be passed.
    /// </summary>
    public const string EndMarker = "--end";

    /// <param name="words">
    /// Everything typed after "w". The last word is the one being completed, unless it is <see cref="EndMarker"/>,
    /// which means the user has finished a word and is starting the next one.
    /// </param>
    public static int Run(IReadOnlyList<string> words, CommandTable table, Services services, TextWriter stdout)
    {
        var atEnd = words.Count == 0 || words[^1] == EndMarker;
        var partial = atEnd ? string.Empty : words[^1];
        var before = words.Take(words.Count - (words.Count == 0 ? 0 : 1)).ToList();

        foreach (var candidate in Candidates(before, partial, table, services)
            .Where(c => c.Text.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(c => c.Text, StringComparer.OrdinalIgnoreCase))
        {
            // One line per suggestion: the text, a tab, then the description the shell shows beside it.
            stdout.WriteLine(candidate.Description is null ? candidate.Text : $"{candidate.Text}\t{candidate.Description}");
        }

        return ExitCodes.Ok;
    }

    private static IEnumerable<Candidate> Candidates(List<string> before, string partial, CommandTable table, Services services)
    {
        var context = new CompletionContext
        {
            Words = [.. before.Skip(1), partial],
            Services = services,
            Table = table,
            Catalog = new AppCatalog(services.Shell, services.AppCache, services.Clock),
        };

        if (before.Count > 0)
        {
            return table.Find(before[0])?.Complete?.Invoke(context) ?? [];
        }

        // The first word: a command, a scene, or an app to open. Apps only once something is typed, because
        // offering every installed app for an empty prompt buries the commands.
        var names = table.All
            .SelectMany(c => c.Aliases
                .Select(a => new Candidate(a, $"short for {c.Name}"))
                .Prepend(new Candidate(c.Name, c.Summary)))
            .Concat(context.SceneNames());

        return partial.Length == 0 ? names : names.Concat(context.AppNames());
    }
}
