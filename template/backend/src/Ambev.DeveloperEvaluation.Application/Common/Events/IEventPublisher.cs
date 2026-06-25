namespace Ambev.DeveloperEvaluation.Application.Common.Events;

/// <summary>
/// Defines a contract for publishing domain events.
/// Decouples handlers from specific messaging systems (Rebus, RabbitMQ, MongoDB logging, etc.).
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Asynchronously publishes a domain event.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="event">The event instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
