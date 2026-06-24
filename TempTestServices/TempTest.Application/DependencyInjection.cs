using Microsoft.Extensions.DependencyInjection;
using TempTest.Application.SensorData;

namespace TempTest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRecordSensorData, RecordSensorData>();

        return services;
    }
}
