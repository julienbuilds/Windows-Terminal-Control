using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wctl.Platform.Windows;

/// <summary>Window and monitor control through Win32.</summary>
public sealed unsafe class Win32Windows : IWindows
{
    [ThreadStatic]
    private static List<nint>? collected;

    public IReadOnlyList<WindowInfo> List()
    {
        var monitors = MonitorsWithHandles();
        var foreground = User32.GetForegroundWindow();
        var windows = new List<WindowInfo>();

        foreach (var hwnd in EnumerateTopLevel())
        {
            if (!IsAppWindow(hwnd))
            {
                continue;
            }

            var processId = ProcessIdOf(hwnd);
            var monitor = User32.MonitorFromWindow(hwnd, User32.MonitorDefaultToNearest);
            var monitorIndex = monitors.Find(m => m.Handle == monitor).Info?.Index ?? 0;
            var image = ProcessImage(processId);

            windows.Add(new WindowInfo(
                hwnd,
                WindowText(hwnd),
                image is null ? "?" : Path.GetFileNameWithoutExtension(image),
                processId,
                monitorIndex,
                User32.IsIconic(hwnd),
                User32.IsZoomed(hwnd),
                (ExStyle(hwnd) & User32.WsExTopMost) != 0,
                hwnd == foreground)
            {
                ExecutablePath = image ?? string.Empty,
            });
        }

        return windows;
    }

    public IReadOnlyList<MonitorInfo> Monitors() => MonitorsWithHandles().Select(m => m.Info).ToList();

    public Rect GetFrame(nint handle)
    {
        RECT rect;
        var hr = Dwmapi.DwmGetWindowAttribute(handle, Dwmapi.DwmwaExtendedFrameBounds, &rect, (uint)sizeof(RECT));
        if (hr != 0 && !User32.GetWindowRect(handle, out rect))
        {
            throw new WctlException($"Could not read the window position (error {Marshal.GetLastPInvokeError()}).");
        }

        return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public void SetFrame(nint handle, Rect frame)
    {
        Apply(handle, frame);

        // A window that lands on a monitor with different scaling resizes itself once. A second pass corrects that.
        var actual = GetFrame(handle);
        if (Math.Abs(actual.Left - frame.Left) > 2 || Math.Abs(actual.Top - frame.Top) > 2
            || Math.Abs(actual.Width - frame.Width) > 2 || Math.Abs(actual.Height - frame.Height) > 2)
        {
            Apply(handle, frame);
        }
    }

    public void Show(nint handle, WindowState state)
    {
        var command = state switch
        {
            WindowState.Minimized => User32.SwMinimize,
            WindowState.Maximized => User32.SwMaximize,
            _ => User32.SwRestore,
        };
        User32.ShowWindow(handle, command);
    }

    public bool Focus(nint handle)
    {
        if (User32.IsIconic(handle))
        {
            User32.ShowWindow(handle, User32.SwRestore);
        }

        if (TryForeground(handle))
        {
            return true;
        }

        // Windows only lets the process that received the last input change the foreground window.
        // Pressing and releasing Alt around the call counts as input. Every window switcher does this.
        User32.keybd_event(User32.VkMenu, 0, 0, 0);
        try
        {
            return TryForeground(handle);
        }
        finally
        {
            User32.keybd_event(User32.VkMenu, 0, User32.KeyEventFKeyUp, 0);
        }
    }

    public void Close(nint handle)
    {
        if (!User32.PostMessageW(handle, User32.WmClose, 0, 0))
        {
            throw new WctlException($"Could not send the close request (error {Marshal.GetLastPInvokeError()}).");
        }
    }

    public void Kill(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            process.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            throw new WctlException($"Process {processId} is not running anymore.");
        }
        catch (InvalidOperationException)
        {
            throw new WctlException($"Process {processId} is not running anymore.");
        }
        catch (Win32Exception e)
        {
            throw new WctlException($"Could not kill process {processId}: {e.Message}");
        }
    }

    public int KillByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            foreach (var process in processes)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Already gone. That is what was asked for.
                }
                catch (Win32Exception e)
                {
                    throw new WctlException($"Could not kill {processName} (process {process.Id}): {e.Message}");
                }
            }

            return processes.Length;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    public void SetTopMost(nint handle, bool enabled)
    {
        var insertAfter = enabled ? User32.HwndTopMost : User32.HwndNoTopMost;
        if (!User32.SetWindowPos(handle, insertAfter, 0, 0, 0, 0, User32.SwpNoMove | User32.SwpNoSize | User32.SwpNoActivate))
        {
            throw new WctlException($"Could not change always on top (error {Marshal.GetLastPInvokeError()}).");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int CollectWindow(nint hwnd, nint lParam)
    {
        collected!.Add(hwnd);
        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int CollectMonitor(nint monitor, nint hdc, RECT* rect, nint lParam)
    {
        collected!.Add(monitor);
        return 1;
    }

    private static List<nint> EnumerateTopLevel()
    {
        var list = new List<nint>(64);
        collected = list;
        try
        {
            if (!User32.EnumWindows(&CollectWindow, 0))
            {
                throw new WctlException($"Windows refused to list windows (error {Marshal.GetLastPInvokeError()}).");
            }
        }
        finally
        {
            collected = null;
        }

        return list;
    }

    private static List<nint> EnumerateChildren(nint parent)
    {
        var list = new List<nint>(16);
        collected = list;
        try
        {
            User32.EnumChildWindows(parent, &CollectWindow, 0);
        }
        finally
        {
            collected = null;
        }

        return list;
    }

    private static List<(nint Handle, MonitorInfo Info)> MonitorsWithHandles()
    {
        var handles = new List<nint>(4);
        collected = handles;
        try
        {
            if (!User32.EnumDisplayMonitors(0, 0, &CollectMonitor, 0))
            {
                throw new WctlException($"Windows refused to list monitors (error {Marshal.GetLastPInvokeError()}).");
            }
        }
        finally
        {
            collected = null;
        }

        var raw = new List<(nint Handle, string Device, Rect Bounds, Rect Work, bool Primary)>(handles.Count);
        foreach (var handle in handles)
        {
            var info = new MONITORINFOEX { Size = (uint)sizeof(MONITORINFOEX) };
            if (!User32.GetMonitorInfoW(handle, ref info))
            {
                throw new WctlException($"Could not read monitor information (error {Marshal.GetLastPInvokeError()}).");
            }

            raw.Add((handle, new string(info.Device), ToRect(info.Monitor), ToRect(info.Work), (info.Flags & User32.MonitorInfoPrimary) != 0));
        }

        // \\.\DISPLAY1, \\.\DISPLAY2, ... is the numbering people see in Windows display settings.
        raw.Sort((a, b) => DisplayNumber(a.Device).CompareTo(DisplayNumber(b.Device)));

        return raw.Select((m, i) => (m.Handle, new MonitorInfo(i + 1, m.Device, m.Bounds, m.Work, m.Primary))).ToList();
    }

    private static int DisplayNumber(string device)
    {
        var digits = device.AsSpan().TrimEnd().ToString().Reverse().TakeWhile(char.IsAsciiDigit).Reverse().ToArray();
        return digits.Length == 0 ? int.MaxValue : int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Rect ToRect(RECT r) => new(r.Left, r.Top, r.Right, r.Bottom);

    private static bool IsAppWindow(nint hwnd)
    {
        if (!User32.IsWindowVisible(hwnd) || User32.GetWindowTextLengthW(hwnd) == 0 || User32.GetWindow(hwnd, User32.GwOwner) != 0)
        {
            return false;
        }

        var exStyle = ExStyle(hwnd);
        var appWindow = (exStyle & User32.WsExAppWindow) != 0;
        if (!appWindow && (exStyle & (User32.WsExToolWindow | User32.WsExNoActivate)) != 0)
        {
            return false;
        }

        var className = ClassName(hwnd);
        if (className is "Progman" or "WorkerW")
        {
            return false;
        }

        return !IsCloaked(hwnd);
    }

    private static bool IsCloaked(nint hwnd)
    {
        int cloaked;
        var hr = Dwmapi.DwmGetWindowAttribute(hwnd, Dwmapi.DwmwaCloaked, &cloaked, sizeof(int));
        return hr == 0 && cloaked != 0;
    }

    private static uint ProcessIdOf(nint hwnd)
    {
        User32.GetWindowThreadProcessId(hwnd, out var processId);

        // Store apps run inside a frame window owned by ApplicationFrameHost. The real app owns the CoreWindow child.
        if (ClassName(hwnd) == "ApplicationFrameWindow")
        {
            foreach (var child in EnumerateChildren(hwnd))
            {
                if (ClassName(child) == "Windows.UI.Core.CoreWindow")
                {
                    User32.GetWindowThreadProcessId(child, out processId);
                    break;
                }
            }
        }

        return processId;
    }

    /// <returns>The full exe path, or null for processes Windows keeps private.</returns>
    private static string? ProcessImage(uint processId)
    {
        var handle = Kernel32.OpenProcess(Kernel32.ProcessQueryLimitedInformation, false, processId);
        if (handle == 0)
        {
            return null;
        }

        try
        {
            var buffer = new char[1024];
            var size = (uint)buffer.Length;
            fixed (char* p = buffer)
            {
                if (!Kernel32.QueryFullProcessImageNameW(handle, 0, p, ref size))
                {
                    return null;
                }
            }

            return new string(buffer, 0, (int)size);
        }
        finally
        {
            Kernel32.CloseHandle(handle);
        }
    }

    private static string WindowText(nint hwnd)
    {
        var length = User32.GetWindowTextLengthW(hwnd);
        if (length == 0)
        {
            return string.Empty;
        }

        var buffer = new char[length + 1];
        fixed (char* p = buffer)
        {
            var copied = User32.GetWindowTextW(hwnd, p, buffer.Length);
            return new string(buffer, 0, copied);
        }
    }

    private static string ClassName(nint hwnd)
    {
        var buffer = stackalloc char[256];
        var copied = User32.GetClassNameW(hwnd, buffer, 256);
        return new string(buffer, 0, copied);
    }

    private static uint ExStyle(nint hwnd) => unchecked((uint)(long)User32.GetWindowLongPtrW(hwnd, User32.GwlExStyle));

    private static bool TryForeground(nint hwnd)
    {
        User32.SetForegroundWindow(hwnd);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (User32.GetForegroundWindow() == hwnd)
            {
                return true;
            }

            Thread.Sleep(20);
        }

        return false;
    }

    private void Apply(nint hwnd, Rect frame)
    {
        if (!User32.GetWindowRect(hwnd, out var window))
        {
            throw new WctlException($"Could not read the window position (error {Marshal.GetLastPInvokeError()}).");
        }

        // The visible frame sits inside the window rect. The difference is the invisible resize border.
        var visible = GetFrame(hwnd);
        var left = visible.Left - window.Left;
        var top = visible.Top - window.Top;
        var right = window.Right - visible.Right;
        var bottom = window.Bottom - visible.Bottom;

        var ok = User32.SetWindowPos(
            hwnd,
            0,
            frame.Left - left,
            frame.Top - top,
            frame.Width + left + right,
            frame.Height + top + bottom,
            User32.SwpNoZOrder | User32.SwpNoActivate);

        if (!ok)
        {
            throw new WctlException($"Windows refused to move the window (error {Marshal.GetLastPInvokeError()}).");
        }
    }
}
