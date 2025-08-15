using System.Collections.Concurrent;

namespace DemoApp.Endpoints;

internal static class GetQueueExecutionLogEndpoint
{
    public static IEndpointRouteBuilder MapGetQueueExecutionLog(this IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .MapGet("/queue", (ConcurrentBag<LogEntry> executionLog)
                => Results.Ok(new
                {
                    JobsExecutionLog = executionLog
                        .OrderBy(x => x.JobEnqueuedAt)
                        .GroupBy(x => x.JobId)
                        .ToDictionary(x => x.Key, x => x.OrderBy(e => e.Timestamp))
                }))
            .WithName("GetQueueExecutionLog")
            .WithDescription("Retrieves the execution log of all jobs in the queue, ordered by their enqueued time.");

        return builder;
    }
}