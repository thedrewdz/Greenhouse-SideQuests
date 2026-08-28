namespace TempTest.Application.SprayEvent;

public interface IRecordSprayEvent
{
    Task<RecordSprayEventResult> RecordAsync(RecordSprayEventCommand command, CancellationToken cancellationToken);
}
