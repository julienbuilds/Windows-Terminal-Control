using System.Text;
using Wctl.Commands;

namespace Wctl.Tests.Commands;

public class ProfileBlockTests
{
    private const string Script = "# script\nRegister-Thing";

    private static readonly UTF8Encoding Utf8 = new(false);

    [Fact]
    public void NoFileYet_CreatesOneWithAByteOrderMark()
    {
        var (bytes, what) = ProfileBlock.Merge([], Script);

        Assert.Equal(ProfileBlock.Result.Created, what);
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var text = Utf8.GetString(bytes[3..]);
        Assert.StartsWith(ProfileBlock.StartMarker, text, StringComparison.Ordinal);
        Assert.Contains("Register-Thing", text, StringComparison.Ordinal);
        Assert.EndsWith(ProfileBlock.EndMarker + "\r\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingProfile_KeepsItsContentAndGetsTheBlockAppended()
    {
        var existing = Utf8.GetBytes("Set-Alias ll Get-ChildItem\r\n");

        var (bytes, what) = ProfileBlock.Merge(existing, Script);

        Assert.Equal(ProfileBlock.Result.Added, what);
        var text = Utf8.GetString(bytes);
        Assert.StartsWith("Set-Alias ll Get-ChildItem\r\n", text, StringComparison.Ordinal);
        Assert.Contains(ProfileBlock.StartMarker, text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingProfileWithoutATrailingLineBreak_GetsABlankLineFirst()
    {
        var existing = Utf8.GetBytes("Set-Alias ll Get-ChildItem");

        var (bytes, _) = ProfileBlock.Merge(existing, Script);

        Assert.Contains("Get-ChildItem\r\n\r\n" + ProfileBlock.StartMarker, Utf8.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void RunningItTwice_ReplacesTheBlockInsteadOfAddingASecond()
    {
        var (first, _) = ProfileBlock.Merge(Utf8.GetBytes("Set-Alias ll Get-ChildItem\r\n"), Script);

        var (second, what) = ProfileBlock.Merge(first, "# script\nRegister-NewerThing");

        Assert.Equal(ProfileBlock.Result.Replaced, what);
        var text = Utf8.GetString(second);
        Assert.Equal(1, Occurrences(text, ProfileBlock.StartMarker));
        Assert.Equal(1, Occurrences(text, ProfileBlock.EndMarker));
        Assert.Contains("Register-NewerThing", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Register-Thing\r", text, StringComparison.Ordinal);
        Assert.StartsWith("Set-Alias ll Get-ChildItem\r\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentAfterTheBlock_Survives()
    {
        var (first, _) = ProfileBlock.Merge(Utf8.GetBytes("before\r\n"), Script);
        var withTail = Utf8.GetBytes(Utf8.GetString(first) + "after\r\n");

        var (second, _) = ProfileBlock.Merge(withTail, Script);

        var text = Utf8.GetString(second);
        Assert.StartsWith("before\r\n", text, StringComparison.Ordinal);
        Assert.EndsWith("after\r\n", text, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(text, ProfileBlock.StartMarker));
    }

    [Fact]
    public void Utf8ProfileWithAByteOrderMark_KeepsIt()
    {
        var existing = (byte[])[.. Encoding.UTF8.GetPreamble(), .. Utf8.GetBytes("# profile\r\n")];

        var (bytes, _) = ProfileBlock.Merge(existing, Script);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        Assert.Contains(ProfileBlock.StartMarker, Utf8.GetString(bytes[3..]), StringComparison.Ordinal);
    }

    [Fact]
    public void Utf16Profile_GetsTheBlockInUtf16AndStaysReadable()
    {
        // This is the case that broke a real profile: appending single byte text to a UTF-16 file.
        var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        var existing = (byte[])[.. utf16.GetPreamble(), .. new UnicodeEncoding(false, false).GetBytes("# profile\r\n")];

        var (bytes, _) = ProfileBlock.Merge(existing, Script);

        var text = new UnicodeEncoding(false, false).GetString(bytes[2..]);
        Assert.StartsWith("# profile\r\n", text, StringComparison.Ordinal);
        Assert.Contains(ProfileBlock.StartMarker, text, StringComparison.Ordinal);
        Assert.Contains("Register-Thing", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\0\0", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Utf16Profile_RunningItTwice_StillReplaces()
    {
        var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        var existing = (byte[])[.. utf16.GetPreamble(), .. new UnicodeEncoding(false, false).GetBytes("# profile\r\n")];
        var (first, _) = ProfileBlock.Merge(existing, Script);

        var (second, what) = ProfileBlock.Merge(first, Script);

        Assert.Equal(ProfileBlock.Result.Replaced, what);
        var text = new UnicodeEncoding(false, false).GetString(second[2..]);
        Assert.Equal(1, Occurrences(text, ProfileBlock.StartMarker));
    }

    private static int Occurrences(string text, string needle)
    {
        var count = 0;
        for (var at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = text.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
