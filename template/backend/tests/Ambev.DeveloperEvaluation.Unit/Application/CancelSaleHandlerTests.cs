using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _handler = new CancelSaleHandler(_saleRepository, _mapper, _eventPublisher);
    }

    [Fact(DisplayName = "Given valid cancel command When handling Then cancels sale and returns success")]
    public async Task Handle_ValidRequest_CancelsSale()
    {
        // Given
        var saleId = Guid.NewGuid();
        var command = new CancelSaleCommand(saleId);

        var sale = new Sale("SALE-1234", Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        sale.AddItem(Guid.NewGuid(), "Product", 2, 10.0m);

        var result = new CancelSaleResult
        {
            Id = sale.Id,
            IsCancelled = true,
            TotalAmount = 0
        };

        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(sale);
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<CancelSaleResult>(Arg.Any<Sale>()).Returns(result);

        // When
        var cancelSaleResult = await _handler.Handle(command, CancellationToken.None);

        // Then
        cancelSaleResult.Should().NotBeNull();
        cancelSaleResult.IsCancelled.Should().BeTrue();
        cancelSaleResult.TotalAmount.Should().Be(0);
        await _saleRepository.Received(1).UpdateAsync(Arg.Is<Sale>(s => s.IsCancelled), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given non-existent sale ID When handling Then throws KeyNotFoundException")]
    public async Task Handle_NonExistentSale_ThrowsKeyNotFoundException()
    {
        // Given
        var saleId = Guid.NewGuid();
        var command = new CancelSaleCommand(saleId);

        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale)null!);

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
