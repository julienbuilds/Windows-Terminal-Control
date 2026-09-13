namespace Wctl;

/// <summary>
/// An error with a message meant for the user. The message is printed as is and the process exits with <see cref="ExitCode"/>.
/// Throw this for expected failures (bad arguments, target not found). Let everything else propagate so it is reported as unexpected.
/// </summary>
public class WctlException(string message, int exitCode = ExitCodes.Failure) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}

/// <summary>No window matched what the user typed. Scenes retry on this while an app they opened is still starting.</summary>
public sealed class WindowNotFoundException(string message) : WctlException(message);
