using System.Text;

namespace Wctl.Commands;

/// <summary>
/// Puts a block of script into a shell profile without disturbing what is already there.
/// It works on bytes rather than text on purpose. A profile can be ANSI, UTF-8 or UTF-16, and appending text
/// in the wrong one produces a file the shell cannot parse, which is exactly the trap this avoids.
/// </summary>
internal static class ProfileBlock
{
    public const string StartMarker = "# >>> Windows Terminal Control completion >>>";
    public const string EndMarker = "# <<< Windows Terminal Control completion <<<";

    /// <summary>What happened, so the command can say it plainly.</summary>
    public enum Result
    {
        Created,
        Added,
        Replaced,
    }

    /// <param name="existing">The current file, or an empty array when there is no file yet.</param>
    /// <param name="script">The script to wrap in markers.</param>
    /// <returns>The bytes the file should now contain.</returns>
    public static (byte[] Bytes, Result What) Merge(byte[] existing, string script)
    {
        var encoding = Detect(existing, out var bom);
        var block = encoding.GetBytes($"{StartMarker}\r\n{script.Trim('\r', '\n')}\r\n{EndMarker}\r\n");

        if (existing.Length == 0)
        {
            // A new file gets a byte order mark, so Windows PowerShell reads it as UTF-8 rather than guessing.
            return ([.. Encoding.UTF8.GetPreamble(), .. block], Result.Created);
        }

        var start = IndexOf(existing, encoding.GetBytes(StartMarker), bom);
        var end = IndexOf(existing, encoding.GetBytes(EndMarker), bom);
        if (start >= 0 && end > start)
        {
            var after = SkipLineBreak(existing, end + encoding.GetBytes(EndMarker).Length, encoding);
            return ([.. existing[..start], .. block, .. existing[after..]], Result.Replaced);
        }

        var separator = encoding.GetBytes(EndsWithLineBreak(existing, encoding) ? "\r\n" : "\r\n\r\n");
        return ([.. existing, .. separator, .. block], Result.Added);
    }

    private static Encoding Detect(byte[] bytes, out int bom)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bom = 3;
            return new UTF8Encoding(false);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            bom = 2;
            return new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            bom = 2;
            return new UnicodeEncoding(bigEndian: true, byteOrderMark: false);
        }

        bom = 0;
        return new UTF8Encoding(false);
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int from)
    {
        for (var i = from; i + needle.Length <= haystack.Length; i++)
        {
            var found = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return i;
            }
        }

        return -1;
    }

    private static int SkipLineBreak(byte[] bytes, int at, Encoding encoding)
    {
        foreach (var candidate in new[] { "\r\n", "\n" })
        {
            var breakBytes = encoding.GetBytes(candidate);
            if (at + breakBytes.Length <= bytes.Length && IndexOf(bytes[at..(at + breakBytes.Length)], breakBytes, 0) == 0)
            {
                return at + breakBytes.Length;
            }
        }

        return at;
    }

    private static bool EndsWithLineBreak(byte[] bytes, Encoding encoding)
    {
        var newline = encoding.GetBytes("\n");
        return bytes.Length >= newline.Length && IndexOf(bytes[^newline.Length..], newline, 0) == 0;
    }
}
