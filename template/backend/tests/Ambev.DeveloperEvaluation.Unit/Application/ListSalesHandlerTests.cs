using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Common.Validation;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class ListSalesHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ListSalesHandler _handler;

    public ListSalesHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new ListSalesHandler(_saleRepository, _mapper);
    }

    [Fact(DisplayName = "Given valid list query When handling Then returns list of sales mapped response")]
    public async Task Handle_ValidQuery_ReturnsPaginatedSales()
    {
        // Given
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var minDate = DateTime.UtcNow.AddDays(-10);
        var maxDate = DateTime.UtcNow;

        var query = new ListSalesQuery(
            page: 1,
            size: 10,
            order: "SaleDate desc",
            customerId: customerId,
            branchId: branchId,
            minDate: minDate,
            maxDate: maxDate
        );

        var salesList = new List<Sale>
        {
            new Sale("SALE-001", customerId, "Customer One", branchId, "Branch One")
        };

        var mappedResult = new List<GetSaleResult>
        {
            new GetSaleResult { Id = salesList[0].Id, SaleNumber = "SALE-001" }
        };

        _saleRepository.ListAsync(
            1, 10, "SaleDate desc", customerId, branchId, minDate, maxDate, Arg.Any<CancellationToken>()
        ).Returns((salesList, 1));

        _mapper.Map<List<GetSaleResult>>(salesList).Returns(mappedResult);

        // When
        var result = await _handler.Handle(query, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data[0].SaleNumber.Should().Be("SALE-001");
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(1);

        await _saleRepository.Received(1).ListAsync(
            1, 10, "SaleDate desc", customerId, branchId, minDate, maxDate, Arg.Any<CancellationToken>()
        );
    }

    [Fact(DisplayName = "Given invalid query parameters When handling Then throws ValidationException")]
    public async Task Handle_InvalidQuery_ThrowsValidationException()
    {
        // Given
        var query = new ListSalesQuery(
            page: 0, // Page less than 1 is invalid
            size: 10,
            order: null
        );

        // When
        var behavior = new ValidationBehavior<ListSalesQuery, ListSalesResult>(new[] { new ListSalesValidator() });
        var act = () => behavior.Handle(query, () => _handler.Handle(query, CancellationToken.None), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<ValidationException>();
    }
}
