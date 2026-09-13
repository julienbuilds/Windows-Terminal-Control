using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Wctl.Platform.Windows;

// Shell COM interfaces and functions, declared by hand so they work in Native AOT.

[GeneratedComInterface]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
internal partial interface IShellItem
{
    /// <returns>A raw interface pointer for <paramref name="iid"/>. Wrap it with <see cref="Com.Wrap{T}"/>.</returns>
    nint BindToHandler(nint bindContext, in Guid handlerId, in Guid iid);

    IShellItem GetParent();

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetDisplayName(uint form);

    uint GetAttributes(uint mask);

    int Compare(IShellItem other, uint hint);
}

[GeneratedComInterface]
[Guid("70629033-E363-4A28-A567-0DB78006E6D7")]
internal partial interface IEnumShellItems
{
    /// <summary>Returns S_OK with one item, or S_FALSE at the end.</summary>
    [PreserveSig]
    int Next(uint count, out nint item, out uint fetched);

    [PreserveSig]
    int Skip(uint count);

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int Clone(out nint copy);
}

internal static partial class Shell32
{
    public const uint SigdnNormalDisplay = 0x00000000;
    public const uint SigdnParentRelativeParsing = 0x80018001;
    public const int SwShowNormal = 1;

    public static readonly Guid BhidEnumItems = new("94F60519-2850-4924-AA5A-D15E84868039");
    public static readonly Guid IidShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
    public static readonly Guid IidEnumShellItems = new("70629033-E363-4A28-A567-0DB78006E6D7");

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SHCreateItemFromParsingName(string path, nint bindContext, in Guid iid, out nint item);

    /// <returns>A value above 32 on success, otherwise an SE_ERR code.</returns>
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint ShellExecuteW(nint hwnd, string? verb, string file, string? parameters, string? directory, int showCommand);
}
