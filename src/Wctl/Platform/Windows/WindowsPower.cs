using System.Runtime.InteropServices;

namespace Wctl.Platform.Windows;

/// <summary>Lock, sleep and awake through Win32.</summary>
public sealed class WindowsPower : IPower
{
    public void Lock()
    {
        if (!PowerInterop.LockWorkStation())
        {
            throw new WctlException($"Windows refused to lock the screen (error {Marshal.GetLastPInvokeError()}).");
        }
    }

    public void Sleep()
    {
        if (PowerInterop.SetSuspendState(0, 0, 0) == 0)
        {
            throw new WctlException($"Windows refused to go to sleep (error {Marshal.GetLastPInvokeError()}).");
        }
    }

    public IDisposable KeepAwake() => new AwakeHold();

    private sealed class AwakeHold : IDisposable
    {
        public AwakeHold()
        {
            var previous = PowerInterop.SetThreadExecutionState(
                PowerInterop.EsContinuous | PowerInterop.EsSystemRequired | PowerInterop.EsDisplayRequired);
            if (previous == 0)
            {
                throw new WctlException("Windows refused the request to stay awake.");
            }
        }

        public void Dispose() => _ = PowerInterop.SetThreadExecutionState(PowerInterop.EsContinuous);
    }
}

internal static partial class PowerInterop
{
    public const uint EsContinuous = 0x80000000;
    public const uint EsSystemRequired = 0x00000001;
    public const uint EsDisplayRequired = 0x00000002;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool LockWorkStation();

    /// <returns>Non zero on success. Returns after the PC wakes up.</returns>
    [LibraryImport("powrprof.dll", SetLastError = true)]
    public static partial byte SetSuspendState(byte hibernate, byte forceCritical, byte disableWakeEvent);

    /// <returns>The previous state, or 0 on failure.</returns>
    [LibraryImport("kernel32.dll")]
    public static partial uint SetThreadExecutionState(uint flags);
}
