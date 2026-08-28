namespace TempTest.Application.FanEvent;

public sealed record RecordFanEventResult(
    Guid Id,
    DateTimeOffset CreatedAtUtc);
