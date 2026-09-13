namespace Wctl;

/// <summary>
/// An error with a message meant for the user. The message is printed as is and the process exits with <see cref="ExitCode"/>.
/// Throw this for expected failures (bad arguments, target not found). Let everything else propagate so it is reported as unexpected.
/// </summary>
public sealed class WctlException(string message, int exitCode = ExitCodes.Failure) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
