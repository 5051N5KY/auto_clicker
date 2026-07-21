using AutoKeyPresser.Models;
using AutoKeyPresser.Services;

namespace AutoKeyPresser.Tests;

public sealed class AutoPressRulesTests
{
    [Theory]
    [InlineData(49, false, false, 0)]
    [InlineData(50, false, true, 50)]
    [InlineData(0.05, true, true, 50)]
    [InlineData(1.5, true, true, 1500)]
    public void IntervalValidationEnforcesMinimum(double value, bool seconds, bool valid, int expected)
    {
        Assert.Equal(valid, AutoPressRules.TryValidateInterval(value, seconds, out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RandomDeviationStaysInsideInclusiveRange()
    {
        var random = new Random(1234);
        var values = Enumerable.Range(0, 500).Select(_ => AutoPressRules.GetNextDelay(1000, true, 100, random));
        Assert.All(values, value => Assert.InRange(value, 900, 1100));
    }

    [Fact]
    public void PressCountLimitIsDetected() =>
        Assert.True(AutoPressRules.ReachedLimit(LimitMode.PressCount, 10, TimeSpan.Zero, 10, TimeSpan.MaxValue));

    [Fact]
    public void DurationLimitIsDetected() =>
        Assert.True(AutoPressRules.ReachedLimit(LimitMode.Duration, 0, TimeSpan.FromSeconds(5), int.MaxValue, TimeSpan.FromSeconds(5)));

    [Theory]
    [InlineData(999, 1000, false)]
    [InlineData(1000, 1000, true)]
    [InlineData(10000, 1000, true)]
    public void DurationMustCoverAtLeastOneInterval(int durationMs, int intervalMs, bool expected) =>
        Assert.Equal(expected, AutoPressRules.IsDurationValid(TimeSpan.FromMilliseconds(durationMs), intervalMs));
}
