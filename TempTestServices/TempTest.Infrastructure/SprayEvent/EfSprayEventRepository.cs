using TempTest.Application.SprayEvent;
using TempTest.Infrastructure.Persistence;

namespace TempTest.Infrastructure.SprayEvent;

public sealed class EfSprayEventRepository(TempTestDbContext dbContext) : ISprayEventRepository
{
    public async Task AddAsync(Domain.SprayEvent.SprayEvent sprayEvent, CancellationToken cancellationToken)
    {
        await dbContext.SprayEvents.AddAsync(sprayEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
