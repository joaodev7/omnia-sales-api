using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents an item within a Sale.
/// Encapsulates pricing, discounts, quantity limits, and cancellation state.
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    public SaleItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the SaleItem class.
    /// </summary>
    public SaleItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        UpdateQuantity(quantity);
    }

    /// <summary>
    /// Updates the item quantity and recalculates pricing and discounts.
    /// </summary>
    public void UpdateQuantity(int quantity)
    {
        if (IsCancelled)
            throw new DomainException("Cannot change the quantity of a cancelled item.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        if (quantity > 20)
            throw new DomainException("It's not possible to sell above 20 identical items.");

        Quantity = quantity;
        CalculateDiscountAndTotals();
    }

    /// <summary>
    /// Updates the unit price and quantity.
    /// </summary>
    public void UpdatePriceAndQuantity(decimal unitPrice, int quantity)
    {
        if (IsCancelled)
            throw new DomainException("Cannot change a cancelled item.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        UnitPrice = unitPrice;
        UpdateQuantity(quantity);
    }

    /// <summary>
    /// Cancels this item. Sets discount and total to 0.
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
        CalculateDiscountAndTotals();
    }

    private void CalculateDiscountAndTotals()
    {
        if (IsCancelled)
        {
            Discount = 0;
            TotalAmount = 0;
            return;
        }

        // Apply discount tiers:
        // - 4+ items: 10% discount
        // - 10-20 items: 20% discount
        // - <4 items: 0% discount
        if (Quantity >= 10)
        {
            Discount = Math.Round(0.20m * UnitPrice * Quantity, 2);
        }
        else if (Quantity >= 4)
        {
            Discount = Math.Round(0.10m * UnitPrice * Quantity, 2);
        }
        else
        {
            Discount = 0;
        }

        TotalAmount = Math.Round((UnitPrice * Quantity) - Discount, 2);
    }
}
