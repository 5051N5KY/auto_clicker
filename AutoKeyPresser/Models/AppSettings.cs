namespace AutoKeyPresser.Models;

public sealed class AppSettings
{
    public int VirtualKey { get; set; } = 0x31;
    public string KeyName { get; set; } = "1";
    public double Interval { get; set; } = 1000;
    public bool IntervalInSeconds { get; set; }
    public bool RandomDeviationEnabled { get; set; }
    public int RandomDeviationMs { get; set; } = 100;
    public int StartDelaySeconds { get; set; } = 3;
    public LimitMode LimitMode { get; set; }
    public int PressCountLimit { get; set; } = 100;
    public double DurationLimitMinutes { get; set; } = 10;
    public bool StopOnOtherKey { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool MinimizeToTray { get; set; } = true;
}
