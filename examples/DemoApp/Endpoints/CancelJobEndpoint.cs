using BusyBee.JobCancellation;

namespace DemoApp.Endpoints;

internal static class CancelJobEndpoint
{
    public static IEndpointRouteBuilder MapCancelJob(this IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder
            .MapPost("/job/cancel/{jobId:guid}", async (
                Guid jobId,
                IJobCancellationService jobCancellationService) =>
            {
                var cancelled = await jobCancellationService.RequestCancellation(jobId);

                return cancelled
                    ? Results.Ok(new { Message = $"Cancellation requested for job with ID {jobId}." })
                    : Results.NotFound(new { Message = $"Job with ID {jobId} not found." });
            })
            .WithName("CancelJob")
            .WithDescription("Requests cancellation of a job with the specified ID.");

        return builder;
    }
}