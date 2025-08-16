using Ignis.Observability;
using Ignis.Queue;
using Ignis.Processor;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ignis;

[PublicAPI]
public static class IgnisModule
{
    public static IgnisBuilder AddIgnis(
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

        return new IgnisBuilder(services);
    }
}