using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _saleRepository.ListAsync(
            request.Page,
            request.Size,
            request.Order,
            request.CustomerId,
            request.BranchId,
            request.MinDate,
            request.MaxDate,
            cancellationToken
        );

        var totalPages = (int)Math.Ceiling((double)totalCount / request.Size);

        return new ListSalesResult
        {
            Data = _mapper.Map<List<GetSaleResult>>(items),
            TotalCount = totalCount,
            CurrentPage = request.Page,
            TotalPages = totalPages
        };
    }
}
