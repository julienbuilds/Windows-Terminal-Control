namespace Wctl.Commands.Apps;

/// <summary>Path text handling without touching the disk, so it can be tested.</summary>
internal static class Paths
{
    /// <summary>Expands ~ and makes the path absolute against the current directory.</summary>
    public static string Resolve(string input, string currentDirectory, string homeDirectory)
    {
        var text = input.Trim().Trim('"');
        if (text == "~")
        {
            return homeDirectory;
        }

        if (text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith(@"~\", StringComparison.Ordinal))
        {
            text = Path.Combine(homeDirectory, text[2..]);
        }

        return Path.GetFullPath(text, currentDirectory);
    }

    /// <summary>True when the text can only mean a path: dots, a tilde, a separator or a drive.</summary>
    public static bool LooksLikePath(string text)
        => text is "." or ".."
        || text.StartsWith('~')
        || text.Contains('\\', StringComparison.Ordinal)
        || text.Contains('/', StringComparison.Ordinal)
        || Path.IsPathRooted(text);
}

/// <summary>Recognizes links.</summary>
internal static class Urls
{
    /// <summary>True for anything with a scheme (https:, mailto:, steam:) and for www. addresses, which get https:// added.</summary>
    public static bool TryNormalize(string text, out string url)
    {
        url = text;
        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + text;
            return true;
        }

        // Drive paths like C:\dev also parse as URIs (scheme "file"), so those are excluded.
        return Uri.TryCreate(text, UriKind.Absolute, out var uri) && !uri.IsFile && !uri.IsUnc && uri.Scheme.Length > 1;
    }
}
