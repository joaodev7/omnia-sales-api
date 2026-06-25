using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a Sale aggregate root in the system.
/// Manages branch, customer details, list of items, discount checks, and cancellation state.
/// </summary>
public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<SaleItem> Items { get; private set; } = new();
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    public Sale()
    {
        SaleDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the Sale class.
    /// </summary>
    public Sale(string saleNumber, Guid customerId, string customerName, Guid branchId, string branchName) : this()
    {
        SaleNumber = saleNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        BranchId = branchId;
        BranchName = branchName;
    }

    /// <summary>
    /// Performs validation of the sale entity using the SaleValidator rules.
    /// </summary>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    /// <summary>
    /// Adds a product to the sale, applying aggregate constraints.
    /// </summary>
    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (IsCancelled)
            throw new DomainException("Cannot add items to a cancelled sale.");

        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId && !i.IsCancelled);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var newItem = new SaleItem(productId, productName, quantity, unitPrice);
            Items.Add(newItem);
        }

        RecalculateTotals();
    }

    /// <summary>
    /// Updates the quantity of a specific item.
    /// </summary>
    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        if (IsCancelled)
            throw new DomainException("Cannot update item quantities on a cancelled sale.");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new DomainException($"Item with ID {itemId} was not found on this sale.");

        item.UpdateQuantity(quantity);
        RecalculateTotals();
    }

    /// <summary>
    /// Removes an item from the sale.
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        if (IsCancelled)
            throw new DomainException("Cannot remove items from a cancelled sale.");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new DomainException($"Item with ID {itemId} was not found on this sale.");

        Items.Remove(item);
        RecalculateTotals();
    }

    /// <summary>
    /// Cancels a specific item within the sale.
    /// </summary>
    public void CancelItem(Guid itemId)
    {
        if (IsCancelled)
            throw new DomainException("Cannot cancel items on an already cancelled sale.");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new DomainException($"Item with ID {itemId} was not found on this sale.");

        item.Cancel();
        RecalculateTotals();
    }

    /// <summary>
    /// Cancels the entire sale, along with all of its items.
    /// </summary>
    public void Cancel()
    {
        if (IsCancelled)
            return;

        IsCancelled = true;
        foreach (var item in Items)
        {
            if (!item.IsCancelled)
            {
                item.Cancel();
            }
        }

        RecalculateTotals();
    }

    /// <summary>
    /// Recalculates the total amount of the sale based on non-cancelled items.
    /// </summary>
    public void RecalculateTotals()
    {
        if (IsCancelled)
        {
            TotalAmount = 0;
            return;
        }

        TotalAmount = Math.Round(Items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount), 2);
    }
}
