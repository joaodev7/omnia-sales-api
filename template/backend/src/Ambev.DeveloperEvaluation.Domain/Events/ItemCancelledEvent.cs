using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class ItemCancelledEvent
{
    public Guid AggregateId { get; }
    public Guid SaleId { get; }
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public DateTime OccurredAt { get; }

    public ItemCancelledEvent(SaleItem item)
    {
        AggregateId = item.Id;
        SaleId = item.SaleId;
        ProductId = item.ProductId;
        ProductName = item.ProductName;
        Quantity = item.Quantity;
        UnitPrice = item.UnitPrice;
        OccurredAt = DateTime.UtcNow;
    }
}
