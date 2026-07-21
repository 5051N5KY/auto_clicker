using AutoKeyPresser.Interop;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Services;

public static class KeyboardEventClassifier
{
    public static bool IsGeneratedByApplication(uint flags, UIntPtr extraInfo) =>
        (flags & NativeMethods.LlkfInjected) != 0 || extraInfo == NativeMethods.InjectionMarker;

    public static bool ShouldStop(int pressedKey, int selectedKey, bool isGenerated, bool enabled, KeyModifiers modifiers = KeyModifiers.None) =>
        enabled && !isGenerated && !KeyCombinationService.IsPartOfCombination(pressedKey, selectedKey, modifiers);
}
