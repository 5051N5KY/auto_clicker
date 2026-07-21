using AutoKeyPresser.Services;

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

    [Fact]
    public void InjectedFlagIsRecognized() =>
        Assert.True(KeyboardEventClassifier.IsGeneratedByApplication(0x10, UIntPtr.Zero));

}
