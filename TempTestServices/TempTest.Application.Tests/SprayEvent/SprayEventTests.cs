namespace TempTest.Application.Tests.SprayEvent;

public sealed class SprayEventTests
{
    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void Create_RejectsStartHumidityOutsidePercentageRange(double humidity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Domain.SprayEvent.SprayEvent.Create(
                18.0m,
                20.0m,
                Convert.ToDecimal(humidity),
                60.0m,
                DateTimeOffset.Parse("2026-06-23T16:45:00Z"),
                DateTimeOffset.Parse("2026-06-23T16:46:00Z"),
                150m,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsEndDateBeforeStartDate()
    {
        Assert.Throws<ArgumentException>(() =>
            Domain.SprayEvent.SprayEvent.Create(
                18.0m,
                20.0m,
                55.0m,
                60.0m,
                DateTimeOffset.Parse("2026-06-23T16:45:00Z"),
                DateTimeOffset.Parse("2026-06-23T16:44:00Z"),
                150m,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsNegativeWaterUsed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Domain.SprayEvent.SprayEvent.Create(
                18.0m,
                20.0m,
                55.0m,
                60.0m,
                DateTimeOffset.Parse("2026-06-23T16:45:00Z"),
                DateTimeOffset.Parse("2026-06-23T16:46:00Z"),
                -1m,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_AcceptsValidSprayEvent()
    {
        DateTimeOffset startDate = DateTimeOffset.Parse("2026-06-23T16:45:00Z");
        DateTimeOffset endDate = DateTimeOffset.Parse("2026-06-23T16:46:00Z");

        Domain.SprayEvent.SprayEvent sprayEvent = Domain.SprayEvent.SprayEvent.Create(
            18.0m,
            20.0m,
            55.0m,
            60.0m,
            startDate,
            endDate,
            150m,
            DateTimeOffset.UtcNow);

        Assert.Equal(18.0m, sprayEvent.StartTemperature);
        Assert.Equal(20.0m, sprayEvent.EndTemperature);
        Assert.Equal(55.0m, sprayEvent.StartHumidity);
        Assert.Equal(60.0m, sprayEvent.EndHumidity);
        Assert.Equal(startDate, sprayEvent.StartDate);
        Assert.Equal(endDate, sprayEvent.EndDate);
        Assert.Equal(150m, sprayEvent.WaterUsedMilliliters);
        Assert.NotEqual(Guid.Empty, sprayEvent.Id);
    }
}
