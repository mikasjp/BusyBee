using Ignis.Queue;
using Ignis.Processor;
using Microsoft.Extensions.DependencyInjection;

namespace Ignis;

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
        services.AddSingleton<Queue.Queue>();
        services.AddSingleton<IBackgroundQueue>(sp => sp.GetRequiredService<Queue.Queue>());
        services.AddHostedService<ProcessorService>();

        return new IgnisBuilder(services);
    }
}