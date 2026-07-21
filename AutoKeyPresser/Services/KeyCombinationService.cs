using AutoKeyPresser.Models;

namespace AutoKeyPresser.Services;

public static class KeyCombinationService
{
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkAlt = 0x12;
    private const int VkLeftShift = 0xA0;
    private const int VkRightShift = 0xA1;
    private const int VkLeftControl = 0xA2;
    private const int VkRightControl = 0xA3;
    private const int VkLeftAlt = 0xA4;
    private const int VkRightAlt = 0xA5;

    public static bool TryGetModifier(int virtualKey, out KeyModifiers modifier)
    {
        modifier = virtualKey switch
        {
            VkShift or VkLeftShift or VkRightShift => KeyModifiers.Shift,
            VkControl or VkLeftControl or VkRightControl => KeyModifiers.Control,
            VkAlt or VkLeftAlt or VkRightAlt => KeyModifiers.Alt,
            _ => KeyModifiers.None
        };
        return modifier != KeyModifiers.None;
    }

    public static IReadOnlyList<int> GetModifierVirtualKeys(KeyModifiers modifiers)
    {
        var keys = new List<int>(3);
        if (modifiers.HasFlag(KeyModifiers.Control)) keys.Add(VkControl);
        if (modifiers.HasFlag(KeyModifiers.Shift)) keys.Add(VkShift);
        if (modifiers.HasFlag(KeyModifiers.Alt)) keys.Add(VkAlt);
        return keys;
    }

    public static bool IsPartOfCombination(int pressedKey, int selectedKey, KeyModifiers modifiers) =>
        pressedKey == selectedKey || TryGetModifier(pressedKey, out var modifier) && modifiers.HasFlag(modifier);

    public static string GetDisplayName(int virtualKey, KeyModifiers modifiers)
    {
        var parts = new List<string>(4);
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        parts.Add(KeyNameService.GetName(virtualKey));
        return string.Join(" + ", parts);
    }

    public static string GetModifierDisplayName(KeyModifiers modifiers)
    {
        var parts = new List<string>(3);
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        return string.Join(" + ", parts);
    }
}
