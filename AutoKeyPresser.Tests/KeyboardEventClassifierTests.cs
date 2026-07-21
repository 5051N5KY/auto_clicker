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
    public void InjectedFlagIsRecognized() =>
        Assert.True(KeyboardEventClassifier.IsGeneratedByApplication(0x10, UIntPtr.Zero));

}
