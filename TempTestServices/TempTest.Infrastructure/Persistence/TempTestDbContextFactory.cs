using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TempTest.Infrastructure.Persistence;

public sealed class TempTestDbContextFactory : IDesignTimeDbContextFactory<TempTestDbContext>
{
    public TempTestDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TempTestServices;Trusted_Connection=True;TrustServerCertificate=True;";

        DbContextOptionsBuilder<TempTestDbContext> options = new();
        options.UseSqlServer(connectionString);

        return new TempTestDbContext(options.Options);
    }
}
