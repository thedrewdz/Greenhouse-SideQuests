namespace TempTest.Application.SensorData;

public sealed class RecordSensorData(ISensorDataRepository repository) : IRecordSensorData
{
    public async Task<RecordSensorDataResult> RecordAsync(
        RecordSensorDataCommand command,
        CancellationToken cancellationToken)
    {
        Domain.SensorData.SensorData sensorData = Domain.SensorData.SensorData.Create(
            command.Temperature,
            command.Humidity,
            command.Timestamp,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(sensorData, cancellationToken);

        return new RecordSensorDataResult(sensorData.Id, sensorData.CreatedAtUtc);
    }
}
