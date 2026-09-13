using System.Globalization;
using System.Text;

namespace Wctl.Commands.Power;

/// <summary>Durations the way people type them: 90m, 1h30m, 45s, or a plain number of minutes.</summary>
internal static class Duration
{
    public static TimeSpan Parse(string text)
    {
        text = text.Trim().ToLowerInvariant();
        if (text.Length == 0)
        {
            throw Invalid(text);
        }

        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
        {
            return Check(TimeSpan.FromMinutes(minutes), text);
        }

        var total = TimeSpan.Zero;
        var number = 0;
        var digits = 0;
        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                number = number * 10 + (c - '0');
                digits++;
                if (digits > 6)
                {
                    throw Invalid(text);
                }

                continue;
            }

            if (digits == 0)
            {
                throw Invalid(text);
            }

            total += c switch
            {
                'h' => TimeSpan.FromHours(number),
                'm' => TimeSpan.FromMinutes(number),
                's' => TimeSpan.FromSeconds(number),
                _ => throw Invalid(text),
            };
            number = 0;
            digits = 0;
        }

        if (digits != 0)
        {
            throw Invalid(text);
        }

        return Check(total, text);
    }

    /// <summary>"1h 30m", "45s", "2m 5s". Rounded to whole seconds.</summary>
    public static string Format(TimeSpan span)
    {
        var seconds = (long)Math.Round(span.TotalSeconds);
        if (seconds <= 0)
        {
            return "0s";
        }

        var sb = new StringBuilder();
        Append(sb, seconds / 3600, "h");
        Append(sb, seconds % 3600 / 60, "m");
        Append(sb, seconds % 60, "s");
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, long value, string unit)
    {
        if (value == 0)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        sb.Append(value.ToString(CultureInfo.InvariantCulture)).Append(unit);
    }

    private static TimeSpan Check(TimeSpan span, string text)
        => span > TimeSpan.Zero ? span : throw new WctlException($"The duration must be more than zero. Got '{text}'.");

    private static WctlException Invalid(string text)
        => new($"Expected a duration like 90m, 1h30m or 45s. Got '{text}'.");
}
