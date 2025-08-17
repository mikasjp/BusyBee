namespace BusyBee.Processor;

public sealed record JobContext(
    Guid JobId, DateTimeOffset QueuedAt, DateTimeOffset StartedAt);