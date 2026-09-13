namespace Wctl.Scenes;

/// <param name="Name">Letters, digits, '-' and '_'. Case does not matter when running.</param>
/// <param name="Steps">Each step is a command line without the leading "w".</param>
public sealed record Scene(string Name, IReadOnlyList<string> Steps);
