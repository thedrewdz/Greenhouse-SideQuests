namespace TempTest.Application.FanEvent;

public sealed class RecordFanEvent(IFanEventRepository repository) : IRecordFanEvent
{
    public async Task<RecordFanEventResult> RecordAsync(
        RecordFanEventCommand command,
        CancellationToken cancellationToken)
    {
        Domain.FanEvent.FanEvent fanEvent = Domain.FanEvent.FanEvent.Create(
            command.StartTemperature,
            command.EndTemperature,
            command.StartHumidity,
            command.EndHumidity,
            command.StartDate,
            command.EndDate,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(fanEvent, cancellationToken);

        return new RecordFanEventResult(fanEvent.Id, fanEvent.CreatedAtUtc);
    }
}
