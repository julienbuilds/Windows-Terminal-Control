namespace Wctl.Platform;

/// <summary>Reads a line typed by the user. Behind an interface so interactive commands can be tested with scripted answers.</summary>
public interface IPrompt
{
    /// <returns>The line without its line ending, or null when input has ended.</returns>
    string? ReadLine(string prompt);
}

public sealed class ConsolePrompt : IPrompt
{
    public string? ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }
}
