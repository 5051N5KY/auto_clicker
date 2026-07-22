namespace AutoKeyPresser.Models;

public sealed class AppSettings
{
    public int VirtualKey { get; set; } = 18;
    public string KeyName { get; set; } = "1";
    public KeyModifiers Modifiers { get; set; }
    public double Interval { get; set; } = 1000;
    public bool IntervalInSeconds { get; set; }
    public bool RandomDeviationEnabled { get; set; }
    public int RandomDeviationMs { get; set; } = 100;
    public int StartDelaySeconds { get; set; } = 3;
    public LimitMode LimitMode { get; set; }
    public int PressCountLimit { get; set; } = 100;
    public double DurationLimit { get; set; } = 10;
    public bool DurationLimitInSeconds { get; set; } = true;
    public bool StopOnOtherKey { get; set; } = true;
}
