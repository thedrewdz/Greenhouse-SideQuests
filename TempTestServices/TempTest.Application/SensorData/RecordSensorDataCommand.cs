namespace TempTest.Application.SensorData;

public sealed record RecordSensorDataCommand(
    decimal Temperature,
    decimal Humidity,
    DateTimeOffset Timestamp);
