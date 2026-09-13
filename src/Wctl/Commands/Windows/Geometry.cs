using Wctl.Platform;

namespace Wctl.Commands.Windows;

/// <summary>Pure rectangle math for placing windows. No Windows calls, fully tested.</summary>
internal static class Geometry
{
    public static Rect LeftHalf(Rect work) => new(work.Left, work.Top, work.Left + work.Width / 2, work.Bottom);

    public static Rect RightHalf(Rect work) => new(work.Left + work.Width / 2, work.Top, work.Right, work.Bottom);

    /// <summary>Centers a window of the given size in the work area. Shrinks it when it does not fit.</summary>
    public static Rect Center(Rect work, int width, int height)
    {
        width = Math.Min(width, work.Width);
        height = Math.Min(height, work.Height);
        var left = work.Left + (work.Width - width) / 2;
        var top = work.Top + (work.Height - height) / 2;
        return Rect.FromSize(left, top, width, height);
    }

    /// <summary>Keeps the window's offset from the top left of the work area, then makes sure it fits in the new one.</summary>
    public static Rect MoveToWorkArea(Rect frame, Rect from, Rect to)
    {
        var moved = Rect.FromSize(to.Left + (frame.Left - from.Left), to.Top + (frame.Top - from.Top), frame.Width, frame.Height);
        return FitInto(moved, to);
    }

    /// <summary>Shrinks the frame to the bounds if needed, then shifts it so it lies inside.</summary>
    public static Rect FitInto(Rect frame, Rect bounds)
    {
        var width = Math.Min(frame.Width, bounds.Width);
        var height = Math.Min(frame.Height, bounds.Height);
        var left = Math.Clamp(frame.Left, bounds.Left, bounds.Right - width);
        var top = Math.Clamp(frame.Top, bounds.Top, bounds.Bottom - height);
        return Rect.FromSize(left, top, width, height);
    }
}
