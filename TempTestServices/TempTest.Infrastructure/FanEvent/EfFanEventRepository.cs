using TempTest.Application.FanEvent;
using TempTest.Infrastructure.Persistence;

namespace TempTest.Infrastructure.FanEvent;

public sealed class EfFanEventRepository(TempTestDbContext dbContext) : IFanEventRepository
{
    public async Task AddAsync(Domain.FanEvent.FanEvent fanEvent, CancellationToken cancellationToken)
    {
        await dbContext.FanEvents.AddAsync(fanEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
