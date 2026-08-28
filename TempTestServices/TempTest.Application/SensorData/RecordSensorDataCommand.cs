namespace TempTest.Application.SensorData;

public sealed record RecordSensorDataCommand(
    decimal Temperature,
    decimal Humidity,
    bool ValveOn,
    bool FanOn,
    DateTimeOffset Timestamp);
