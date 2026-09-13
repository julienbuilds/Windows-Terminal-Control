using System.Reflection;

namespace Wctl.Cli;

public static class VersionInfo
{
    /// <summary>The version set at build time (from the csproj or -p:Version), without the commit suffix.</summary>
    public static string Current { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var informational = typeof(VersionInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
        {
            throw new InvalidOperationException("The assembly has no version. The build is broken.");
        }

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? informational : informational[..plus];
    }
}
