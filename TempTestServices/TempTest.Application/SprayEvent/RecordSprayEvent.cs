namespace TempTest.Application.SprayEvent;

public sealed class RecordSprayEvent(ISprayEventRepository repository) : IRecordSprayEvent
{
    public async Task<RecordSprayEventResult> RecordAsync(
        RecordSprayEventCommand command,
        CancellationToken cancellationToken)
    {
        Domain.SprayEvent.SprayEvent sprayEvent = Domain.SprayEvent.SprayEvent.Create(
            command.StartTemperature,
            command.EndTemperature,
            command.StartHumidity,
            command.EndHumidity,
            command.StartDate,
            command.EndDate,
            command.WaterUsedMilliliters,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(sprayEvent, cancellationToken);

        return new RecordSprayEventResult(sprayEvent.Id, sprayEvent.CreatedAtUtc);
    }
}
