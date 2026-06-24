namespace TempTest.Application.SensorData;

public interface IRecordSensorData
{
    Task<RecordSensorDataResult> RecordAsync(RecordSensorDataCommand command, CancellationToken cancellationToken);
}
