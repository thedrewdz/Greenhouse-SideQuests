using TempTest.Application.FanEvent;

namespace TempTest.Application.Tests.FanEvent;

public sealed class RecordFanEventTests
{
    [Fact]
    public async Task RecordAsync_PersistsFanEventThroughRepository()
    {
        FanEventRepositorySpy repository = new();
        RecordFanEvent recorder = new(repository);
        DateTimeOffset startDate = DateTimeOffset.Parse("2026-06-23T16:45:00Z");
        DateTimeOffset endDate = DateTimeOffset.Parse("2026-06-23T16:46:00Z");

        RecordFanEventResult result = await recorder.RecordAsync(
            new RecordFanEventCommand(18.0m, 20.0m, 55.0m, 60.0m, startDate, endDate),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(repository.SavedFanEvents);

        Domain.FanEvent.FanEvent saved = repository.SavedFanEvents[0];
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(18.0m, saved.StartTemperature);
        Assert.Equal(20.0m, saved.EndTemperature);
        Assert.Equal(55.0m, saved.StartHumidity);
        Assert.Equal(60.0m, saved.EndHumidity);
        Assert.Equal(startDate, saved.StartDate);
        Assert.Equal(endDate, saved.EndDate);
    }

    private sealed class FanEventRepositorySpy : IFanEventRepository
    {
        public List<Domain.FanEvent.FanEvent> SavedFanEvents { get; } = [];

        public Task AddAsync(Domain.FanEvent.FanEvent fanEvent, CancellationToken cancellationToken)
        {
            SavedFanEvents.Add(fanEvent);

            return Task.CompletedTask;
        }
    }
}
