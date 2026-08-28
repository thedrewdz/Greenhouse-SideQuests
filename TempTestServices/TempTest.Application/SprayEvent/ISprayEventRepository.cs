namespace TempTest.Application.SprayEvent;

public interface ISprayEventRepository
{
    Task AddAsync(Domain.SprayEvent.SprayEvent sprayEvent, CancellationToken cancellationToken);
}
