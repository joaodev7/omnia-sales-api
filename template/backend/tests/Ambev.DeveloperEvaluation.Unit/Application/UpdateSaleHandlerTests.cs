using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;
    private readonly UpdateSaleHandler _handler;

    public UpdateSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _handler = new UpdateSaleHandler(_saleRepository, _mapper, _eventPublisher);
    }

    [Fact(DisplayName = "Given valid update command When handling Then updates sale items and prices")]
    public async Task Handle_ValidRequest_UpdatesSaleCorrectly()
    {
        // Given
        var saleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var command = new UpdateSaleCommand
        {
            Id = saleId,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Updated Customer",
            BranchId = Guid.NewGuid(),
            BranchName = "Updated Branch",
            Items = new List<UpdateSaleItemCommand>
            {
                new()
                {
                    ProductId = productId,
                    ProductName = "Product",
                    Quantity = 8,
                    UnitPrice = 15.0m,
                    IsCancelled = false
                }
            }
        };

        var sale = new Sale("SALE-1234", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        sale.AddItem(productId, "Product", 2, 10.0m); // Old values

        var result = new UpdateSaleResult
        {
            Id = sale.Id,
            TotalAmount = 108.0m // 8 * 15 * 0.9 = 108.0
        };

        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<UpdateSaleResult>(Arg.Any<Sale>()).Returns(result);

        // When
        var updateSaleResult = await _handler.Handle(command, CancellationToken.None);

        // Then
        updateSaleResult.Should().NotBeNull();
        updateSaleResult.TotalAmount.Should().Be(108.0m);
        await _saleRepository.Received(1).UpdateAsync(Arg.Is<Sale>(s =>
            s.CustomerId == command.CustomerId &&
            s.CustomerName == command.CustomerName &&
            s.Items.First().Quantity == 8 &&
            s.Items.First().UnitPrice == 15.0m
        ), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given command trying to reactivate cancelled item When handling Then throws DomainException")]
    public async Task Handle_ReactivateCancelledItem_ThrowsDomainException()
    {
        // Given
        var saleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var command = new UpdateSaleCommand
        {
            Id = saleId,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Updated Customer",
            BranchId = Guid.NewGuid(),
            BranchName = "Updated Branch",
            Items = new List<UpdateSaleItemCommand>
            {
                new()
                {
                    ProductId = productId,
                    ProductName = "Product",
                    Quantity = 5,
                    UnitPrice = 10.0m,
                    IsCancelled = false // Requesting active state
                }
            }
        };

        var sale = new Sale("SALE-1234", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        sale.AddItem(productId, "Product", 2, 10.0m);
        sale.Items.First().Cancel(); // Item is cancelled in DB

        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>();
    }
}
