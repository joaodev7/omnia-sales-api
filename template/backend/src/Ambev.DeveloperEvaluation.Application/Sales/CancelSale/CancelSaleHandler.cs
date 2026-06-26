using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, CancelSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;

    public CancelSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IEventPublisher eventPublisher)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<CancelSaleResult> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch existing
        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        // 3. Cancel sale
        sale.Cancel();

        // 4. Save
        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);

        // 5. Publish Events
        await _eventPublisher.PublishAsync(new SaleCancelledEvent(updatedSale), cancellationToken);

        foreach (var item in updatedSale.Items)
        {
            await _eventPublisher.PublishAsync(new ItemCancelledEvent(item), cancellationToken);
        }

        // 6. Return response
        return _mapper.Map<CancelSaleResult>(updatedSale);
    }
}
