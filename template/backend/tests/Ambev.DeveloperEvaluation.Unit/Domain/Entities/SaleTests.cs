using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "Validation should pass for valid sale data")]
    public void Given_ValidSaleData_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var sale = SaleTestData.GenerateValidSale();

        // Act
        var result = sale.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "Validation should fail for invalid sale data")]
    public void Given_InvalidSaleData_When_Validated_Then_ShouldReturnInvalid()
    {
        // Arrange
        var sale = new Sale
        {
            SaleNumber = "", // Invalid
            CustomerName = "", // Invalid
            BranchName = "" // Invalid
        };

        // Act
        var result = sale.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Theory(DisplayName = "Discounts should be calculated correctly based on quantity")]
    [InlineData(1, 10.0, 0.0)]    // < 4 items: 0% discount
    [InlineData(3, 10.0, 0.0)]    // < 4 items: 0% discount
    [InlineData(4, 10.0, 4.0)]    // >= 4 items: 10% discount -> 4 * 10 * 0.1 = 4.0
    [InlineData(9, 10.0, 9.0)]    // >= 4 items: 10% discount -> 9 * 10 * 0.1 = 9.0
    [InlineData(10, 10.0, 20.0)]  // >= 10 items: 20% discount -> 10 * 10 * 0.2 = 20.0
    [InlineData(20, 10.0, 40.0)]  // >= 10 items: 20% discount -> 20 * 10 * 0.2 = 40.0
    public void Given_ItemQuantity_When_Calculated_Then_DiscountShouldMatchTier(int quantity, decimal unitPrice, decimal expectedDiscount)
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        var productId = Guid.NewGuid();

        // Act
        sale.AddItem(productId, "Product", quantity, unitPrice);
        var item = sale.Items.First();

        // Assert
        Assert.Equal(expectedDiscount, item.Discount);
        Assert.Equal((quantity * unitPrice) - expectedDiscount, item.TotalAmount);
    }

    [Fact(DisplayName = "Adding more than 20 items of the same product should throw DomainException")]
    public void Given_MoreThan20Items_When_Added_Then_ShouldThrowDomainException()
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        var productId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<DomainException>(() => sale.AddItem(productId, "Product", 21, 10.0m));
    }

    [Fact(DisplayName = "Adding item that pushes total quantity above 20 should throw DomainException")]
    public void Given_ExistingItem_When_AddingMoreThan20Total_Then_ShouldThrowDomainException()
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        var productId = Guid.NewGuid();

        // Act
        sale.AddItem(productId, "Product", 15, 10.0m);

        // Assert
        Assert.Throws<DomainException>(() => sale.AddItem(productId, "Product", 6, 10.0m)); // Total 21 -> throws
    }

    [Fact(DisplayName = "Cancelling a sale should cancel all items and set total amount to 0")]
    public void Given_SaleWithItems_When_Cancelled_Then_AllItemsCancelledAndTotalIsZero()
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        sale.AddItem(Guid.NewGuid(), "Prod1", 5, 10.0m); // 50 - 5 = 45
        sale.AddItem(Guid.NewGuid(), "Prod2", 10, 20.0m); // 200 - 40 = 160
        Assert.Equal(205.0m, sale.TotalAmount);

        // Act
        sale.Cancel();

        // Assert
        Assert.True(sale.IsCancelled);
        Assert.Equal(0.0m, sale.TotalAmount);
        Assert.All(sale.Items, item => Assert.True(item.IsCancelled));
        Assert.All(sale.Items, item => Assert.Equal(0.0m, item.Discount));
        Assert.All(sale.Items, item => Assert.Equal(0.0m, item.TotalAmount));
    }

    [Fact(DisplayName = "Cancelling an item should update the overall sale total amount")]
    public void Given_SaleWithMultipleItems_When_OneItemCancelled_Then_TotalAmountUpdated()
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        var prod1Id = Guid.NewGuid();
        var prod2Id = Guid.NewGuid();
        sale.AddItem(prod1Id, "Prod1", 5, 10.0m); // 50 - 5 = 45
        sale.AddItem(prod2Id, "Prod2", 10, 20.0m); // 200 - 40 = 160
        Assert.Equal(205.0m, sale.TotalAmount);

        var itemToCancel = sale.Items.First(i => i.ProductId == prod1Id);

        // Act
        sale.CancelItem(itemToCancel.Id);

        // Assert
        Assert.True(itemToCancel.IsCancelled);
        Assert.False(sale.IsCancelled);
        Assert.Equal(160.0m, sale.TotalAmount); // Only Prod2 total remains
    }

    [Fact(DisplayName = "Updating active item price and quantity should recalculate discounts and totals")]
    public void Given_ActiveItem_When_PriceAndQuantityUpdated_Then_DiscountAndTotalsRecalculated()
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        var prodId = Guid.NewGuid();
        sale.AddItem(prodId, "Product", 2, 10.0m); // Total: 20, Discount: 0
        var item = sale.Items.First();

        // Act
        item.UpdatePriceAndQuantity(20.0m, 5); // Quantity 5 -> 10% discount -> 5 * 20 * 0.1 = 10 discount. Total: 90.

        // Assert
        Assert.Equal(20.0m, item.UnitPrice);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(10.0m, item.Discount);
        Assert.Equal(90.0m, item.TotalAmount);
    }

    [Fact(DisplayName = "Updating cancelled item price and quantity should throw DomainException")]
    public void Given_CancelledItem_When_PriceAndQuantityUpdated_Then_ShouldThrowDomainException()
    {
        // Arrange
        var sale = new Sale("SALE-TEST-001", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        var prodId = Guid.NewGuid();
        sale.AddItem(prodId, "Product", 2, 10.0m);
        var item = sale.Items.First();
        item.Cancel();

        // Act & Assert
        Assert.Throws<DomainException>(() => item.UpdatePriceAndQuantity(20.0m, 5));
    }
}
