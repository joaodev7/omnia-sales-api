using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

public static class SaleTestData
{
    private static readonly Faker<Sale> SaleFaker = new Faker<Sale>()
        .RuleFor(s => s.SaleNumber, f => $"SALE-{f.Date.Recent().ToString("yyyyMMdd")}-{f.Random.AlphaNumeric(8).ToUpper()}")
        .RuleFor(s => s.CustomerId, f => Guid.NewGuid())
        .RuleFor(s => s.CustomerName, f => f.Name.FullName())
        .RuleFor(s => s.BranchId, f => Guid.NewGuid())
        .RuleFor(s => s.BranchName, f => $"{f.Address.City()} Branch");

    public static Sale GenerateValidSale()
    {
        var sale = SaleFaker.Generate();
        // Add a default valid item
        sale.AddItem(Guid.NewGuid(), "Default Product", 2, 10.0m);
        return sale;
    }

    public static Sale GenerateSaleWithItems(params (Guid productId, string productName, int quantity, decimal unitPrice)[] items)
    {
        var sale = SaleFaker.Generate();
        foreach (var item in items)
        {
            sale.AddItem(item.productId, item.productName, item.quantity, item.unitPrice);
        }
        return sale;
    }

    public static SaleItem GenerateValidSaleItem()
    {
        return new SaleItem(Guid.NewGuid(), "Test Product", 2, 15.0m);
    }
}
