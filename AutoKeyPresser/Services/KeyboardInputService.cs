using System.ComponentModel;
using System.Runtime.InteropServices;
using AutoKeyPresser.Interop;

namespace AutoKeyPresser.Services;

public sealed class KeyboardInputService
{
    public async Task PressAsync(int virtualKey, CancellationToken cancellationToken)
    {
        Send(virtualKey, keyUp: false);
        try
        {
            await Task.Delay(40, cancellationToken);
        }
        finally
        {
            Send(virtualKey, keyUp: true);
        }
    }

    private static void Send(int virtualKey, bool keyUp)
    {
        var input = new NativeMethods.Input
        {
            type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KeybdInput
                {
                    wVk = checked((ushort)virtualKey),
                    dwFlags = keyUp ? NativeMethods.KeyEventFKeyUp : 0,
                    dwExtraInfo = NativeMethods.InjectionMarker
                }
            }
        };

        if (NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.Input>()) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not send the key input.");
    }
}
