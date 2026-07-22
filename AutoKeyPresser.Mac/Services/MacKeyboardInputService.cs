using AutoKeyPresser.Mac.Interop;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Mac.Services;

public sealed class MacKeyboardInputService
{
    public async Task PressAsync(int key, KeyModifiers modifiers, CancellationToken token)
    {
        var modifierKeys = new List<ushort>();
        if (modifiers.HasFlag(KeyModifiers.Control)) modifierKeys.Add(MacKeyService.Control);
        if (modifiers.HasFlag(KeyModifiers.Shift)) modifierKeys.Add(MacKeyService.Shift);
        if (modifiers.HasFlag(KeyModifiers.Alt)) modifierKeys.Add(MacKeyService.Option);
        foreach (var modifier in modifierKeys) Post(modifier, true);
        Post((ushort)key, true);
        try { await Task.Delay(40, token); }
        finally
        {
            Post((ushort)key, false);
            for (var i = modifierKeys.Count - 1; i >= 0; i--) Post(modifierKeys[i], false);
        }
    }

    private static void Post(ushort key, bool down)
    {
        var ev = MacNative.CGEventCreateKeyboardEvent(IntPtr.Zero, key, down);
        if (ev == IntPtr.Zero) throw new InvalidOperationException("macOS could not create the keyboard event.");
        try
        {
            MacNative.CGEventSetIntegerValueField(ev, MacNative.EventSourceUserData, MacNative.InjectionMarker);
            MacNative.CGEventPost(0, ev);
        }
        finally { MacNative.CFRelease(ev); }
    }
}
