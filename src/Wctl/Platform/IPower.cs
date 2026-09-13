namespace Wctl.Platform;

/// <summary>Lock, sleep, and keeping the PC awake.</summary>
public interface IPower
{
    /// <summary>Locks the screen, like Win+L.</summary>
    void Lock();

    /// <summary>Puts the PC to sleep. Returns when it wakes up again.</summary>
    void Sleep();

    /// <summary>Stops the PC and the display from sleeping until the returned handle is disposed.</summary>
    IDisposable KeepAwake();
}

/// <summary>Time, so commands that wait can be tested without waiting.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }

    /// <returns>False when <paramref name="token"/> was cancelled before the time was up.</returns>
    bool Delay(TimeSpan duration, CancellationToken token);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public bool Delay(TimeSpan duration, CancellationToken token) => !token.WaitHandle.WaitOne(duration);
}
