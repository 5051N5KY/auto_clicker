using AutoKeyPresser.Interop;

namespace AutoKeyPresser.Services;

public static class KeyboardEventClassifier
{
    public static bool IsGeneratedByApplication(uint flags, UIntPtr extraInfo) =>
        (flags & NativeMethods.LlkfInjected) != 0 || extraInfo == NativeMethods.InjectionMarker;

    public static bool ShouldStop(int pressedKey, int selectedKey, bool isGenerated, bool enabled) =>
        enabled && !isGenerated && pressedKey != selectedKey;
}
