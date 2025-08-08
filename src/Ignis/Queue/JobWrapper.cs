using System.Diagnostics;
using Ignis.Processor;

namespace Ignis.Queue;

internal sealed record JobWrapper(
    Guid JobId,
    TimeSpan? Timeout,
    DateTimeOffset QueuedAt,
    ActivityContext? ActivityContext,
    Func<IServiceProvider, JobContext, CancellationToken, Task> Job);