using System.Diagnostics.Metrics;

namespace Ignis.Observability;

internal sealed class Metrics(IMeterFactory meterFactory)
{
    public UpDownCounter<int> ActiveJobsCounter { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateUpDownCounter<int>("ignis.processor.active_jobs",
            description: "The number of active jobs being processed by the Ignis processor");

    public Counter<int> TotalProcessedJobsCounter { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateCounter<int>("ignis.processor.total_processed_jobs",
            description: "The total number of jobs processed by the Ignis processor");

    public Counter<int> TotalSuccessfulJobsCounter { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateCounter<int>("ignis.processor.total_successful_jobs",
            description: "The total number of jobs that were successfully processed");

    public Counter<int> TotalFailedJobsCounter { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateCounter<int>("ignis.processor.total_failed_jobs",
            description: "The total number of jobs that failed during processing");

    public Counter<int> TotalTimedOutJobsCounter { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateCounter<int>("ignis.processor.total_timed_out_jobs",
            description: "The total number of jobs that timed out during processing");

    public Histogram<long> JobProcessingDurationHistogram { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateHistogram<long>("ignis.processor.job_processing_duration",
            description: "The duration of job processing in milliseconds",
            unit: "ms");

    public Histogram<double> WaitingTimeHistogram { get; } = meterFactory
        .Create(MetricsConstants.MeterName)
        .CreateHistogram<double>("ignis.processor.job_waiting_time",
            description: "The waiting time for jobs in the queue in milliseconds",
            unit: "ms");
}