using System.Windows.Input;

namespace AutoKeyPresser.Services;

public static class KeyNameService
{
    public static string GetName(int virtualKey)
    {
        var key = KeyInterop.KeyFromVirtualKey(virtualKey);
        return key switch
        {
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Escape",
            Key.Left => "Left Arrow",
            Key.Right => "Right Arrow",
            Key.Up => "Up Arrow",
            Key.Down => "Down Arrow",
            Key.LeftCtrl => "Left Ctrl",
            Key.RightCtrl => "Right Ctrl",
            Key.LeftShift => "Left Shift",
            Key.RightShift => "Right Shift",
            Key.LeftAlt => "Left Alt",
            Key.RightAlt => "Right Alt",
            Key.NumPad0 or Key.NumPad1 or Key.NumPad2 or Key.NumPad3 or Key.NumPad4 or Key.NumPad5 or Key.NumPad6 or Key.NumPad7 or Key.NumPad8 or Key.NumPad9 => "Num " + key.ToString()[^1],
            _ => key.ToString()
        };
    }
}
