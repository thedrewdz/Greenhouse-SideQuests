using TempTest.Application.SensorData;
using TempTest.Infrastructure.Persistence;

namespace TempTest.Infrastructure.SensorData;

public sealed class EfSensorDataRepository(TempTestDbContext dbContext) : ISensorDataRepository
{
    public async Task AddAsync(Domain.SensorData.SensorData sensorData, CancellationToken cancellationToken)
    {
        await dbContext.SensorData.AddAsync(sensorData, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
