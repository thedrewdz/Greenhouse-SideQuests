using Microsoft.Extensions.DependencyInjection;
using TempTest.Application.FanEvent;
using TempTest.Application.SensorData;
using TempTest.Application.SprayEvent;

namespace TempTest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRecordSensorData, RecordSensorData>();
        services.AddScoped<IRecordSprayEvent, RecordSprayEvent>();
        services.AddScoped<IRecordFanEvent, RecordFanEvent>();

        return services;
    }
}
