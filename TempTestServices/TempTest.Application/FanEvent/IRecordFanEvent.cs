namespace TempTest.Application.FanEvent;

public interface IRecordFanEvent
{
    Task<RecordFanEventResult> RecordAsync(RecordFanEventCommand command, CancellationToken cancellationToken);
}
