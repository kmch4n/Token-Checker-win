namespace UsageBeacon.Utilities;

public enum PollingInterval
{
    Sec30  = 30,
    Min1   = 60,
    Min2   = 120,
    Min3   = 180,
    Min5   = 300,
    Min10  = 600,
}

public static class PollingIntervalExtensions
{
    public static TimeSpan ToTimeSpan(this PollingInterval p)
        => TimeSpan.FromSeconds((int)p);

    public static readonly PollingInterval Default = PollingInterval.Min5;

    public static readonly PollingInterval[] All =
    [
        PollingInterval.Min2,
        PollingInterval.Min3,
        PollingInterval.Min5,
        PollingInterval.Min10,
    ];
}
