namespace Wctl.Cli;

/// <summary>
/// One tab completion suggestion. The description is what the shell shows next to it in the completion menu,
/// so it should be a few words, not a sentence.
/// </summary>
/// <param name="Text">What gets typed when the suggestion is picked.</param>
internal sealed record Candidate(string Text, string? Description = null)
{
    public static implicit operator Candidate(string text) => new(text);
}
