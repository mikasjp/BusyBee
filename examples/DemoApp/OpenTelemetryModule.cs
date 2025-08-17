using BusyBee.Observability;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace DemoApp;

internal static class OpenTelemetryModule
{
    private const string LoggingExporterName = "logging";
    private const string TracingExporterName = "tracing";
    private const string MetricsExporterName = "metrics";

    public static void AddOpenTelemetryModule(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddOpenTelemetry()
            .WithLogging(logging => logging
                .AddOtlpExporter(LoggingExporterName, _ => { }))
            .WithTracing(tracing => tracing
                .AddSource(TracingConstants.TraceSourceName)
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(TracingExporterName, _ => { }))
            .WithMetrics(metrics => metrics
                .AddMeter(MetricsConstants.MeterName)
                .AddPrometheusExporter(MetricsExporterName, _ => { }));

        builder.Services.Configure<OtlpExporterOptions>(LoggingExporterName,
            builder.Configuration.GetSection(nameof(OtlpExporterOptions)).GetSection(LoggingExporterName));
        builder.Services.Configure<OtlpExporterOptions>(TracingExporterName,
            builder.Configuration.GetSection(nameof(OtlpExporterOptions)).GetSection(TracingExporterName));
        builder.Services.Configure<OtlpExporterOptions>(MetricsExporterName,
            builder.Configuration.GetSection(nameof(OtlpExporterOptions)).GetSection(MetricsExporterName));

        builder.Services.AddLogging(logging =>
            logging.AddOpenTelemetry(x =>
            {
                x.IncludeScopes = true;
                x.IncludeFormattedMessage = true;
            }));
    }
}