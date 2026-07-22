using System.Runtime.InteropServices;

namespace AutoKeyPresser.Mac.Interop;

internal static class MacNative
{
    private const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    internal const int KeyDown = 10;
    internal const int KeyUp = 11;
    internal const int FlagsChanged = 12;
    internal const int KeyboardEventKeycode = 9;
    internal const int EventSourceUserData = 42;
    internal const long InjectionMarker = 0x415550524D4143;
    internal const ulong ShiftFlag = 1UL << 17;
    internal const ulong ControlFlag = 1UL << 18;
    internal const ulong OptionFlag = 1UL << 19;

    internal delegate IntPtr EventTapCallback(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo);

    [DllImport(ApplicationServices)] internal static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, bool keyDown);
    [DllImport(ApplicationServices)] internal static extern void CGEventPost(int tap, IntPtr eventRef);
    [DllImport(ApplicationServices)] internal static extern void CGEventSetIntegerValueField(IntPtr eventRef, int field, long value);
    [DllImport(ApplicationServices)] internal static extern long CGEventGetIntegerValueField(IntPtr eventRef, int field);
    [DllImport(ApplicationServices)] internal static extern ulong CGEventGetFlags(IntPtr eventRef);
    [DllImport(ApplicationServices)] internal static extern IntPtr CGEventTapCreate(int tap, int place, int options, ulong mask, EventTapCallback callback, IntPtr userInfo);
    [DllImport(ApplicationServices)] internal static extern void CGEventTapEnable(IntPtr tap, bool enable);
    [DllImport(ApplicationServices)] [return: MarshalAs(UnmanagedType.I1)] internal static extern bool CGPreflightListenEventAccess();
    [DllImport(ApplicationServices)] [return: MarshalAs(UnmanagedType.I1)] internal static extern bool CGRequestListenEventAccess();
    [DllImport(ApplicationServices)] [return: MarshalAs(UnmanagedType.I1)] internal static extern bool CGPreflightPostEventAccess();
    [DllImport(ApplicationServices)] [return: MarshalAs(UnmanagedType.I1)] internal static extern bool CGRequestPostEventAccess();

    [DllImport(CoreFoundation)] internal static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, nint order);
    [DllImport(CoreFoundation)] internal static extern IntPtr CFRunLoopGetCurrent();
    [DllImport(CoreFoundation)] internal static extern void CFRunLoopAddSource(IntPtr runLoop, IntPtr source, IntPtr mode);
    [DllImport(CoreFoundation)] internal static extern void CFRunLoopRun();
    [DllImport(CoreFoundation)] internal static extern void CFRunLoopStop(IntPtr runLoop);
    [DllImport(CoreFoundation)] internal static extern void CFRelease(IntPtr value);

    internal static IntPtr DefaultRunLoopMode
    {
        get
        {
            var handle = NativeLibrary.Load(CoreFoundation);
            return NativeLibrary.GetExport(handle, "kCFRunLoopDefaultMode") is var symbol
                ? Marshal.ReadIntPtr(symbol)
                : IntPtr.Zero;
        }
    }
}
