namespace TempTest.Application.Tests.SensorData;

public sealed class SensorDataTests
{
    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void Create_RejectsHumidityOutsidePercentageRange(double humidity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Domain.SensorData.SensorData.Create(
                12.3m,
                Convert.ToDecimal(humidity),
                false,
                false,
                DateTimeOffset.Parse("2026-06-23T16:45:00Z"),
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_AcceptsValidSensorData()
    {
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-06-23T16:45:00Z");

        Domain.SensorData.SensorData sensorData = Domain.SensorData.SensorData.Create(
            12.3m,
            64.1m,
            true,
            false,
            timestamp,
            DateTimeOffset.UtcNow);

        Assert.Equal(12.3m, sensorData.Temperature);
        Assert.Equal(64.1m, sensorData.Humidity);
        Assert.True(sensorData.ValveOn);
        Assert.False(sensorData.FanOn);
        Assert.Equal(timestamp, sensorData.Timestamp);
        Assert.NotEqual(Guid.Empty, sensorData.Id);
    }
}
