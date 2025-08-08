using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Ignis.Processor;

public sealed class ProcessorOptions
{
    /// <summary>
    /// The maximum number of jobs that can be processed concurrently.
    /// If not specified, the processor will execute jobs sequentially.
    /// If specified, it must be greater than or equal to 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Batch size must be greater than or equal to 1")]
    public int? JobsBatchSize { get; set; }

    /// <summary>
    /// The maximum time a job can run before it is considered timed out.
    /// If not specified, there is no timeout.
    /// If specified, it must be a positive <see cref="TimeSpan"/>.
    /// </summary>
    [JobTimeoutValidation(ErrorMessage = "Job timeout must be greater than zero")]
    public TimeSpan? JobTimeout { get; set; }

    /// <summary>
    /// The log level to use when logging job timeouts.
    /// If not specified, the default log level is <see cref="LogLevel.None"/>.
    /// </summary>
    public LogLevel? JobTimeoutLogLevel { get; set; }
}

internal sealed class JobTimeoutValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not TimeSpan timeSpan)
        {
            return true;
        }

        return timeSpan > TimeSpan.Zero && timeSpan <= TimeSpan.MaxValue;
    }
}