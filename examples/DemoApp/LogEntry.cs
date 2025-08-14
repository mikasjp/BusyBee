namespace DemoApp;

public record LogEntry(DateTimeOffset Timestamp, DateTimeOffset JobEnqueuedAt, Guid JobId, string Message);