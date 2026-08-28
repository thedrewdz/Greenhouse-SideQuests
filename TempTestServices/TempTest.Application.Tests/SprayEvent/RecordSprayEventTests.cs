using TempTest.Application.SprayEvent;

namespace TempTest.Application.Tests.SprayEvent;

public sealed class RecordSprayEventTests
{
    [Fact]
    public async Task RecordAsync_PersistsSprayEventThroughRepository()
    {
        SprayEventRepositorySpy repository = new();
        RecordSprayEvent recorder = new(repository);
        DateTimeOffset startDate = DateTimeOffset.Parse("2026-06-23T16:45:00Z");
        DateTimeOffset endDate = DateTimeOffset.Parse("2026-06-23T16:46:00Z");

        RecordSprayEventResult result = await recorder.RecordAsync(
            new RecordSprayEventCommand(18.0m, 20.0m, 55.0m, 60.0m, startDate, endDate, 150m),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(repository.SavedSprayEvents);

        Domain.SprayEvent.SprayEvent saved = repository.SavedSprayEvents[0];
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(18.0m, saved.StartTemperature);
        Assert.Equal(20.0m, saved.EndTemperature);
        Assert.Equal(55.0m, saved.StartHumidity);
        Assert.Equal(60.0m, saved.EndHumidity);
        Assert.Equal(startDate, saved.StartDate);
        Assert.Equal(endDate, saved.EndDate);
        Assert.Equal(150m, saved.WaterUsedMilliliters);
    }

    private sealed class SprayEventRepositorySpy : ISprayEventRepository
    {
        public List<Domain.SprayEvent.SprayEvent> SavedSprayEvents { get; } = [];

        public Task AddAsync(Domain.SprayEvent.SprayEvent sprayEvent, CancellationToken cancellationToken)
        {
            SavedSprayEvents.Add(sprayEvent);

            return Task.CompletedTask;
        }
    }
}
