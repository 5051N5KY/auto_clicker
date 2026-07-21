using AutoKeyPresser.Models;

namespace AutoKeyPresser.Services;

public static class AutoPressRules
{
    public static bool TryValidateInterval(double value, bool seconds, out int milliseconds)
    {
        var computed = seconds ? value * 1000d : value;
        if (double.IsNaN(computed) || double.IsInfinity(computed) || computed < 50 || computed > int.MaxValue)
        {
            milliseconds = 0;
            return false;
        }
        milliseconds = (int)Math.Round(computed);
        return true;
    }

    public static int GetNextDelay(int intervalMs, bool randomEnabled, int deviationMs, Random random)
    {
        if (!randomEnabled || deviationMs == 0) return intervalMs;
        var min = Math.Max(1, intervalMs - deviationMs);
        var max = Math.Min(int.MaxValue, (long)intervalMs + deviationMs);
        return random.NextInt64(min, max + 1) is var result ? (int)result : intervalMs;
    }

    public static bool ReachedLimit(LimitMode mode, long presses, TimeSpan elapsed, int pressLimit, TimeSpan durationLimit) =>
        mode == LimitMode.PressCount && presses >= pressLimit ||
        mode == LimitMode.Duration && elapsed >= durationLimit;
}
