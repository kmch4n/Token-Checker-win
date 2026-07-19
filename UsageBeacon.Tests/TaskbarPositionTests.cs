using UsageBeacon.Utilities;

namespace UsageBeacon.Tests;

public sealed class TaskbarPositionTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    [Fact]
    public void ShouldRescan_ReturnsTrue_WhenGeometryChanged()
        => Assert.True(TaskbarPosition.ShouldRescan(
            Now, Now, geometryChanged: true, Interval));

    [Fact]
    public void ShouldRescan_ReturnsTrue_WhenCacheEntryIsStale()
        => Assert.True(TaskbarPosition.ShouldRescan(
            Now, Now.AddSeconds(-6), geometryChanged: false, Interval));

    [Fact]
    public void ShouldRescan_ReturnsFalse_WhenEntryIsFreshAndGeometryUnchanged()
        => Assert.False(TaskbarPosition.ShouldRescan(
            Now, Now.AddSeconds(-1), geometryChanged: false, Interval));
}
