using System.Runtime.InteropServices;
using AutoKeyPresser.Interop;

namespace AutoKeyPresser.Tests;

public sealed class NativeLayoutTests
{
    [Fact]
    public void InputHasWindowsExpectedSize()
    {
        var expected = Environment.Is64BitProcess ? 40 : 28;
        Assert.Equal(expected, Marshal.SizeOf<NativeMethods.Input>());
    }
}
