using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.ORM.Events;

/// <summary>
/// Infrastructure implementation of IEventPublisher.
/// Logs events via ILogger and persists them to MongoDB for auditing purposes.
/// </summary>
public class LoggingEventPublisher : IEventPublisher
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly ILogger<LoggingEventPublisher> _logger;

    public LoggingEventPublisher(IMongoDatabase mongoDatabase, ILogger<LoggingEventPublisher> logger)
    {
        _mongoDatabase = mongoDatabase;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var eventType = typeof(T).Name;
        var timestamp = DateTime.UtcNow;
        var serializedPayload = JsonSerializer.Serialize(@event);

        // Extract Aggregate ID via reflection
        Guid aggregateId = Guid.Empty;
        var saleProp = @event.GetType().GetProperty("Sale");
        if (saleProp != null && saleProp.GetValue(@event) is Sale sale)
        {
            aggregateId = sale.Id;
        }
        else
        {
            var itemProp = @event.GetType().GetProperty("Item");
            if (itemProp != null && itemProp.GetValue(@event) is SaleItem item)
            {
                aggregateId = item.Id;
            }
        }

        // Log using standard logger
        _logger.LogInformation("Domain Event Published: {EventType} for Aggregate {AggregateId}. Payload: {Payload}", 
            eventType, aggregateId, serializedPayload);

        // Persist to MongoDB audit trail
        try
        {
            var auditCollection = _mongoDatabase.GetCollection<BsonDocument>("SalesEventsAudit");
            var auditDoc = new BsonDocument
            {
                { "EventType", eventType },
                { "AggregateId", aggregateId.ToString() },
                { "Timestamp", timestamp },
                { "SerializedPayload", serializedPayload }
            };

            await auditCollection.InsertOneAsync(auditDoc, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit event to MongoDB.");
            // Do not fail the transactional operation if auditing logging fails, 
            // depending on business requirements (here we log and suppress to avoid blocking).
        }
    }
}
