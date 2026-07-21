using AutoKeyPresser.Services;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Tests;

public sealed class KeyboardEventClassifierTests
{
    [Fact]
    public void SelectedKeyDoesNotStop() =>
        Assert.False(KeyboardEventClassifier.ShouldStop(0x31, 0x31, false, true));

    [Fact]
    public void InjectedEventIsIgnored() =>
        Assert.False(KeyboardEventClassifier.ShouldStop(0x57, 0x31, true, true));

    [Fact]
    public void OtherPhysicalKeyStops() =>
        Assert.True(KeyboardEventClassifier.ShouldStop(0x57, 0x31, false, true));

    [Theory]
    [InlineData(0x10)]
    [InlineData(0xA0)]
    [InlineData(0xA1)]
    public void SelectedShiftDoesNotStopCombination(int shiftKey) =>
        Assert.False(KeyboardEventClassifier.ShouldStop(shiftKey, 0x46, false, true, KeyModifiers.Shift));

    [Fact]
    public void UnselectedModifierStopsCombination() =>
        Assert.True(KeyboardEventClassifier.ShouldStop(0x11, 0x46, false, true, KeyModifiers.Shift));

    [Fact]
    public void CombinationNameIsReadable() =>
        Assert.Equal("Ctrl + Shift + Q", KeyCombinationService.GetDisplayName(0x51, KeyModifiers.Control | KeyModifiers.Shift));

    [Fact]
    public void ReleasedModifierIsRemovedFromPendingCombination() =>
        Assert.Equal(KeyModifiers.Control, KeyCombinationService.RemoveModifier(KeyModifiers.Control | KeyModifiers.Shift, 0xA0));

    [Theory]
    [InlineData(0x31, "1")]
    [InlineData(0x41, "A")]
    [InlineData(0x65, "Num 5")]
    [InlineData(0x70, "F1")]
    [InlineData(0x20, "Space")]
    [InlineData(0x25, "Left Arrow")]
    [InlineData(0x08, "Backspace")]
    public void KeyNamesAreUserFriendly(int virtualKey, string expected) =>
        Assert.Equal(expected, KeyNameService.GetName(virtualKey));

    [Fact]
    public void ShiftNumFiveIsNormalizedFromClear() =>
        Assert.Equal(0x65, KeyCombinationService.NormalizeMainKey(0x0C, KeyModifiers.Shift));

    [Fact]
    public void InjectedFlagIsRecognized() =>
        Assert.True(KeyboardEventClassifier.IsGeneratedByApplication(0x10, UIntPtr.Zero));

}
