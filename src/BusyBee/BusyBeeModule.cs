using BusyBee.Observability;
using BusyBee.Processor;
using BusyBee.Queue;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BusyBee;

[PublicAPI]
public static class BusyBeeModule
{
    public static BusyBeeBuilder AddBusyBee(
        this IServiceCollection services,
        Action<QueueOptions>? configureQueue = null,
        Action<ProcessorOptions>? configureProcessor = null)
    {
        services
            .AddOptions<QueueOptions>()
            .Configure(configureQueue ?? (_ => { }))
            .ValidateDataAnnotations();
        services
            .AddOptions<ProcessorOptions>()
            .Configure(configureProcessor ?? (_ => { }))
            .ValidateDataAnnotations();
        services.TryAddSingleton<Queue.Queue>();
        services.TryAddSingleton<IBackgroundQueue>(sp => sp.GetRequiredService<Queue.Queue>());
        services.AddHostedService<ProcessorService>();
        services.TryAddSingleton<SlotsTracker>();
        services.TryAddSingleton<JobRunner>();
        services.TryAddSingleton<Metrics>();

        return new BusyBeeBuilder(services);
    }
}