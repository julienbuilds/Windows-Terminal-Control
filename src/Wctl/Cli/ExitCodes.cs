namespace Wctl.Cli;

/// <summary>Process exit codes. Scripts can rely on these.</summary>
public static class ExitCodes
{
    /// <summary>The command did what was asked.</summary>
    public const int Ok = 0;

    /// <summary>The command could not do what was asked: bad arguments, target not found, unsupported hardware.</summary>
    public const int Failure = 1;

    /// <summary>Something went wrong that the tool did not expect. Run again with --debug for the stack trace.</summary>
    public const int Unexpected = 2;
}
