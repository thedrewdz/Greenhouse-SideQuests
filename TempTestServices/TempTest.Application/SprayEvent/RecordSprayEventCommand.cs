namespace TempTest.Application.SprayEvent;

public sealed record RecordSprayEventCommand(
    decimal StartTemperature,
    decimal EndTemperature,
    decimal StartHumidity,
    decimal EndHumidity,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal WaterUsedMilliliters);
