namespace Ignis.Queue.Exceptions;

public sealed class QueueCapacityExceededException()
    : Exception("The queue has reached its maximum capacity and cannot accept more jobs");