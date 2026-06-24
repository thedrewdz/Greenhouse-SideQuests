using TempTest.Application.SensorData;

namespace TempTest.Application.Tests.SensorData;

public sealed class RecordSensorDataTests
{
    [Fact]
    public async Task RecordAsync_PersistsSensorDataThroughRepository()
    {
        SensorDataRepositorySpy repository = new();
        RecordSensorData recorder = new(repository);
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-06-23T16:45:00Z");

        RecordSensorDataResult result = await recorder.RecordAsync(
            new RecordSensorDataCommand(12.3m, 64.1m, timestamp),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(repository.SavedSensorData);

        Domain.SensorData.SensorData saved = repository.SavedSensorData[0];
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(12.3m, saved.Temperature);
        Assert.Equal(64.1m, saved.Humidity);
        Assert.Equal(timestamp, saved.Timestamp);
    }

    private sealed class SensorDataRepositorySpy : ISensorDataRepository
    {
        public List<Domain.SensorData.SensorData> SavedSensorData { get; } = [];

        public Task AddAsync(Domain.SensorData.SensorData sensorData, CancellationToken cancellationToken)
        {
            SavedSensorData.Add(sensorData);

            return Task.CompletedTask;
        }
    }
}
