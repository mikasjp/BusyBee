using System.Diagnostics;

namespace Ignis.Queue;

internal sealed record JobWrapper(
    Guid JobId,
    ActivityContext? ActivityContext,
    Func<IServiceProvider, CancellationToken, Task> Job);