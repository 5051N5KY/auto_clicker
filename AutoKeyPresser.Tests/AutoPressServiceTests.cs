using AutoKeyPresser.Models;
using AutoKeyPresser.Services;

namespace AutoKeyPresser.Tests;

public sealed class AutoPressServiceTests
{
    [Fact]
    public async Task ConcurrentStopOnlySucceedsOnce()
    {
        using var service = new AutoPressService(new KeyboardInputService());
        var options = new AutoPressOptions(0x31, 1000, false, 0, 10_000, LimitMode.None, 1, TimeSpan.Zero);
        Assert.True(service.Start(options));

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => service.Stop(StopReason.Manual))));
        await service.StopAndWaitAsync(StopReason.Manual);

        Assert.Equal(1, results.Count(value => value));
        Assert.False(service.IsRunning);
    }
}
