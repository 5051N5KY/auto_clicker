using AutoKeyPresser.Mac.Services;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Tests;

public sealed class MacKeyServiceTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(18, "1")]
    [InlineData(49, "Space")]
    [InlineData(83, "Num 1")]
    [InlineData(122, "F1")]
    [InlineData(123, "Left Arrow")]
    public void UsesFriendlyAppleKeyNames(ushort key, string expected) =>
        Assert.Equal(expected, MacKeyService.Name(key));

    [Fact]
    public void FormatsOptionCombination() =>
        Assert.Equal("Shift + Option + 2", MacKeyService.Display(19, KeyModifiers.Shift | KeyModifiers.Alt));

    [Theory]
    [InlineData(56, KeyModifiers.Shift)]
    [InlineData(59, KeyModifiers.Control)]
    [InlineData(58, KeyModifiers.Alt)]
    public void RecognizesAppleModifiers(ushort key, KeyModifiers expected)
    {
        Assert.True(MacKeyService.TryGetModifier(key, out var actual));
        Assert.Equal(expected, actual);
    }
}
