namespace TempTest.Application.SensorData;

public sealed record RecordSensorDataResult(
    Guid Id,
    DateTimeOffset CreatedAtUtc);
