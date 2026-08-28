namespace TempTest.Application.Tests.FanEvent;

public sealed class FanEventTests
{
    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void Create_RejectsStartHumidityOutsidePercentageRange(double humidity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Domain.FanEvent.FanEvent.Create(
                18.0m,
                20.0m,
                Convert.ToDecimal(humidity),
                60.0m,
                DateTimeOffset.Parse("2026-06-23T16:45:00Z"),
                DateTimeOffset.Parse("2026-06-23T16:46:00Z"),
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsEndDateBeforeStartDate()
    {
        Assert.Throws<ArgumentException>(() =>
            Domain.FanEvent.FanEvent.Create(
                18.0m,
                20.0m,
                55.0m,
                60.0m,
                DateTimeOffset.Parse("2026-06-23T16:45:00Z"),
                DateTimeOffset.Parse("2026-06-23T16:44:00Z"),
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_AcceptsValidFanEvent()
    {
        DateTimeOffset startDate = DateTimeOffset.Parse("2026-06-23T16:45:00Z");
        DateTimeOffset endDate = DateTimeOffset.Parse("2026-06-23T16:46:00Z");

        Domain.FanEvent.FanEvent fanEvent = Domain.FanEvent.FanEvent.Create(
            18.0m,
            20.0m,
            55.0m,
            60.0m,
            startDate,
            endDate,
            DateTimeOffset.UtcNow);

        Assert.Equal(18.0m, fanEvent.StartTemperature);
        Assert.Equal(20.0m, fanEvent.EndTemperature);
        Assert.Equal(55.0m, fanEvent.StartHumidity);
        Assert.Equal(60.0m, fanEvent.EndHumidity);
        Assert.Equal(startDate, fanEvent.StartDate);
        Assert.Equal(endDate, fanEvent.EndDate);
        Assert.NotEqual(Guid.Empty, fanEvent.Id);
    }
}
