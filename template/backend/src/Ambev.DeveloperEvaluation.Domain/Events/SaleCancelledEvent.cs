using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class SaleCancelledEvent
{
    public Guid AggregateId { get; }
    public string SaleNumber { get; }
    public DateTime OccurredAt { get; }

    public SaleCancelledEvent(Sale sale)
    {
        AggregateId = sale.Id;
        SaleNumber = sale.SaleNumber;
        OccurredAt = DateTime.UtcNow;
    }
}
