namespace TempTest.Application.SensorData;

public interface ISensorDataRepository
{
    Task AddAsync(Domain.SensorData.SensorData sensorData, CancellationToken cancellationToken);
}
