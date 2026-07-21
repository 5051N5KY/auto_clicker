namespace AutoKeyPresser.Models;

public sealed record AutoPressOptions(
    int VirtualKey,
    KeyModifiers Modifiers,
    int IntervalMs,
    bool RandomDeviationEnabled,
    int RandomDeviationMs,
    int StartDelayMs,
    LimitMode LimitMode,
    int PressCountLimit,
    TimeSpan DurationLimit);
