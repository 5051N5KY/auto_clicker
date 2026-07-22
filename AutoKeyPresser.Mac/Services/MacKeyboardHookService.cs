using AutoKeyPresser.Mac.Interop;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Mac.Services;

public sealed class MacKeyboardHookService : IDisposable
{
    private readonly MacNative.EventTapCallback _callback;
    private Thread? _thread;
    private IntPtr _tap;
    private IntPtr _source;
    private IntPtr _runLoop;
    public event Action<ushort>? KeyDown;
    public event Action<ushort>? KeyUp;

    public MacKeyboardHookService() => _callback = OnEvent;

    public bool EnsurePermissions()
    {
        var listen = MacNative.CGPreflightListenEventAccess() || MacNative.CGRequestListenEventAccess();
        var post = MacNative.CGPreflightPostEventAccess() || MacNative.CGRequestPostEventAccess();
        return listen && post;
    }

    public void Start()
    {
        if (_thread is not null) return;
        _thread = new Thread(Run) { IsBackground = true, Name = "macOS keyboard event tap" };
        _thread.Start();
    }

    private void Run()
    {
        var mask = (1UL << MacNative.KeyDown) | (1UL << MacNative.KeyUp) | (1UL << MacNative.FlagsChanged);
        _tap = MacNative.CGEventTapCreate(1, 0, 1, mask, _callback, IntPtr.Zero);
        if (_tap == IntPtr.Zero) return;
        _source = MacNative.CFMachPortCreateRunLoopSource(IntPtr.Zero, _tap, 0);
        _runLoop = MacNative.CFRunLoopGetCurrent();
        MacNative.CFRunLoopAddSource(_runLoop, _source, MacNative.DefaultRunLoopMode);
        MacNative.CGEventTapEnable(_tap, true);
        MacNative.CFRunLoopRun();
    }

    private IntPtr OnEvent(IntPtr proxy, int type, IntPtr ev, IntPtr userInfo)
    {
        if (MacNative.CGEventGetIntegerValueField(ev, MacNative.EventSourceUserData) == MacNative.InjectionMarker) return ev;
        var key = (ushort)MacNative.CGEventGetIntegerValueField(ev, MacNative.KeyboardEventKeycode);
        if (type == MacNative.KeyDown) KeyDown?.Invoke(key);
        else if (type == MacNative.KeyUp) KeyUp?.Invoke(key);
        else if (type == MacNative.FlagsChanged && MacKeyService.TryGetModifier(key, out var modifier))
        {
            var flags = MacNative.CGEventGetFlags(ev);
            var down = modifier switch
            {
                KeyModifiers.Shift => (flags & MacNative.ShiftFlag) != 0,
                KeyModifiers.Control => (flags & MacNative.ControlFlag) != 0,
                _ => (flags & MacNative.OptionFlag) != 0
            };
            if (down) KeyDown?.Invoke(key); else KeyUp?.Invoke(key);
        }
        return ev;
    }

    public void Dispose()
    {
        if (_runLoop != IntPtr.Zero) MacNative.CFRunLoopStop(_runLoop);
        if (_source != IntPtr.Zero) MacNative.CFRelease(_source);
        if (_tap != IntPtr.Zero) MacNative.CFRelease(_tap);
    }
}
