using Wctl.Platform;

namespace Wctl.Tests;

/// <summary>Scripted answers for interactive commands. When the answers run out, input ends.</summary>
internal sealed class FakePrompt : IPrompt
{
    public Queue<string> Answers { get; } = new();

    public List<string> Prompts { get; } = [];

    public FakePrompt Answer(params string[] lines)
    {
        foreach (var line in lines)
        {
            Answers.Enqueue(line);
        }

        return this;
    }

    public string? ReadLine(string prompt)
    {
        Prompts.Add(prompt);
        return Answers.Count > 0 ? Answers.Dequeue() : null;
    }
}
