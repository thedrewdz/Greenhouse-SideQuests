namespace TempTest.Application.FanEvent;

public sealed record RecordFanEventCommand(
    decimal StartTemperature,
    decimal EndTemperature,
    decimal StartHumidity,
    decimal EndHumidity,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate);
