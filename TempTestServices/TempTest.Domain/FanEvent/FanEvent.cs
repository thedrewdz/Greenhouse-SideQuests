namespace TempTest.Domain.FanEvent;

public sealed class FanEvent
{
    private FanEvent()
    {
    }

    private FanEvent(
        decimal startTemperature,
        decimal endTemperature,
        decimal startHumidity,
        decimal endHumidity,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        StartTemperature = startTemperature;
        EndTemperature = endTemperature;
        StartHumidity = startHumidity;
        EndHumidity = endHumidity;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public decimal StartTemperature { get; private set; }

    public decimal EndTemperature { get; private set; }

    public decimal StartHumidity { get; private set; }

    public decimal EndHumidity { get; private set; }

    public DateTimeOffset StartDate { get; private set; }

    public DateTimeOffset EndDate { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static FanEvent Create(
        decimal startTemperature,
        decimal endTemperature,
        decimal startHumidity,
        decimal endHumidity,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        DateTimeOffset createdAtUtc)
    {
        if (startHumidity is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(startHumidity), "Start humidity must be between 0 and 100.");
        }

        if (endHumidity is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(endHumidity), "End humidity must be between 0 and 100.");
        }

        if (startDate == default)
        {
            throw new ArgumentException("Start date is required.", nameof(startDate));
        }

        if (endDate == default)
        {
            throw new ArgumentException("End date is required.", nameof(endDate));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("End date must not be before start date.", nameof(endDate));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Created timestamp is required.", nameof(createdAtUtc));
        }

        return new FanEvent(
            startTemperature,
            endTemperature,
            startHumidity,
            endHumidity,
            startDate,
            endDate,
            createdAtUtc);
    }
}
