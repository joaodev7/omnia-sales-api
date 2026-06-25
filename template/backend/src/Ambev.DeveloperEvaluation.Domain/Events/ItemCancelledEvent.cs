using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class ItemCancelledEvent
{
    public SaleItem Item { get; }

    public ItemCancelledEvent(SaleItem item)
    {
        Item = item;
    }
}
