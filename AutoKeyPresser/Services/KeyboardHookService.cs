using System.ComponentModel;
using System.Runtime.InteropServices;
using AutoKeyPresser.Interop;

namespace AutoKeyPresser.Services;

public sealed class KeyboardHookService : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private IntPtr _hook;
    public event Action<int>? PhysicalKeyDown;

    public KeyboardHookService() => _callback = HookCallback;

    public void Start()
    {
        if (_hook != IntPtr.Zero) return;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, _callback, NativeMethods.GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "The global keyboard hook could not be started.");
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && (wParam == (IntPtr)NativeMethods.WmKeyDown || wParam == (IntPtr)NativeMethods.WmSysKeyDown))
        {
            var data = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
            if (!KeyboardEventClassifier.IsGeneratedByApplication(data.flags, data.dwExtraInfo))
                PhysicalKeyDown?.Invoke((int)data.vkCode);
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
