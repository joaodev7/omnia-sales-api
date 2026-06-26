using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class SaleModifiedEvent
{
    public Guid AggregateId { get; }
    public string SaleNumber { get; }
    public Guid CustomerId { get; }
    public string CustomerName { get; }
    public Guid BranchId { get; }
    public string BranchName { get; }
    public decimal TotalAmount { get; }
    public DateTime OccurredAt { get; }

    public SaleModifiedEvent(Sale sale)
    {
        AggregateId = sale.Id;
        SaleNumber = sale.SaleNumber;
        CustomerId = sale.CustomerId;
        CustomerName = sale.CustomerName;
        BranchId = sale.BranchId;
        BranchName = sale.BranchName;
        TotalAmount = sale.TotalAmount;
        OccurredAt = DateTime.UtcNow;
    }
}
