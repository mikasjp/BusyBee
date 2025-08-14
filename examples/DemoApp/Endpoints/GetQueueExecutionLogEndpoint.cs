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
            .WithName("GetQueueExecutionLog");

        return builder;
    }
}