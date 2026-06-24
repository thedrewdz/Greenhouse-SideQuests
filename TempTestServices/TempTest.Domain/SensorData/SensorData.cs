namespace TempTest.Domain.SensorData;

public sealed class SensorData
{
    private SensorData()
    {
    }

    private SensorData(decimal temperature, decimal humidity, DateTimeOffset timestamp, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        Temperature = temperature;
        Humidity = humidity;
        Timestamp = timestamp;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public decimal Temperature { get; private set; }

    public decimal Humidity { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static SensorData Create(decimal temperature, decimal humidity, DateTimeOffset timestamp, DateTimeOffset createdAtUtc)
    {
        if (humidity is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(humidity), "Humidity must be between 0 and 100.");
        }

        if (timestamp == default)
        {
            throw new ArgumentException("Timestamp is required.", nameof(timestamp));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Created timestamp is required.", nameof(createdAtUtc));
        }

        return new SensorData(temperature, humidity, timestamp, createdAtUtc);
    }
}
