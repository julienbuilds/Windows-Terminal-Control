using Wctl.Platform;

namespace Wctl.Tests;

internal sealed class FakePower : IPower
{
    public int Locked { get; private set; }

    public int Slept { get; private set; }

    public int AwakeAcquired { get; private set; }

    public int AwakeReleased { get; private set; }

    public void Lock() => Locked++;

    public void Sleep() => Slept++;

    public IDisposable KeepAwake()
    {
        AwakeAcquired++;
        return new Hold(this);
    }

    private sealed class Hold(FakePower owner) : IDisposable
    {
        public void Dispose() => owner.AwakeReleased++;
    }
}

/// <summary>Time that only moves when someone waits. <see cref="StopAt"/> acts like Ctrl+C at that moment.</summary>
internal sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = new(2026, 9, 13, 12, 0, 0, TimeSpan.FromHours(2));

    public DateTimeOffset? StopAt { get; set; }

    public int Delays { get; private set; }

    public bool Delay(TimeSpan duration, CancellationToken token)
    {
        Delays++;
        var target = Now + duration;
        if (StopAt is not null && target >= StopAt)
        {
            Now = StopAt.Value;
            return false;
        }

        Now = target;
        return true;
    }
}
