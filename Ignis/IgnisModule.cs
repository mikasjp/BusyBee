using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace Ignis;

public static class IgnisModule
{
    public static IServiceCollection AddIgnis(this IServiceCollection services, Action<QueueOptions> configure)
    {
        services
            .AddOptions<QueueOptions>()
            .Configure(configure)
            .ValidateDataAnnotations();
        services.AddSingleton<Queue.Queue>();
        services.AddSingleton<IBackgroundQueue>(sp => sp.GetRequiredService<Queue.Queue>());
        services.AddHostedService<Processor.ProcessorService>();

        return services;
    }
}