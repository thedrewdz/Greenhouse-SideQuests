namespace TempTest.Application.FanEvent;

public interface IFanEventRepository
{
    Task AddAsync(Domain.FanEvent.FanEvent fanEvent, CancellationToken cancellationToken);
}
