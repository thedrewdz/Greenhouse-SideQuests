using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TempTest.Application.SensorData;
using TempTest.Infrastructure.Persistence;
using TempTest.Infrastructure.SensorData;

namespace TempTest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("SensorData")
            ?? configuration["SqlConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure SQL connection string is required. Configure ConnectionStrings:SensorData or SqlConnectionString.");
        }

        services.AddDbContext<TempTestDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ISensorDataRepository, EfSensorDataRepository>();

        return services;
    }
}
