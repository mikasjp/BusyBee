using System.Diagnostics;
using BusyBee.Processor;

namespace BusyBee.Queue;

internal sealed record JobWrapper(
    Guid JobId,
    TimeSpan? Timeout,
    DateTimeOffset QueuedAt,
    ActivityContext? ActivityContext,
    Func<IServiceProvider, JobContext, CancellationToken, Task> Job);