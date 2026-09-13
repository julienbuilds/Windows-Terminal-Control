using System.Globalization;

namespace Wctl.Commands;

/// <summary>Parses "&lt;n&gt;", "+&lt;n&gt;" and "-&lt;n&gt;" for percent values like volume and brightness.</summary>
internal static class Level
{
    /// <param name="what">Name for error messages, for example "Volume".</param>
    /// <returns>The new value, 0 to 100. Relative changes are clamped, absolute values must be in range.</returns>
    public static int Apply(string arg, int current, string what)
    {
        if (arg.Length > 1 && arg[0] is '+' or '-' && TryParse(arg.AsSpan(1), out var delta))
        {
            return Math.Clamp(arg[0] == '+' ? current + delta : current - delta, 0, 100);
        }

        if (TryParse(arg, out var value))
        {
            if (value > 100)
            {
                throw new WctlException($"{what} must be between 0 and 100. Got {value}.");
            }

            return value;
        }

        throw new WctlException($"Expected a number, +<n> or -<n> for {what.ToLowerInvariant()}. Got '{arg}'.");
    }

    private static bool TryParse(ReadOnlySpan<char> text, out int value)
        => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
