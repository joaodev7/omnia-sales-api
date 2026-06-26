using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;

    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IEventPublisher eventPublisher)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        // 1. Retrieve existing Sale
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale with ID {command.Id} not found");

        // 2. Reconcile items using rich domain method
        var mappedItems = command.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.IsCancelled));
        var cancelledItems = sale.ReconcileItems(
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName,
            mappedItems
        );

        // 3. Save changes
        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);

        // 4. Publish Event
        await _eventPublisher.PublishAsync(new SaleModifiedEvent(updatedSale), cancellationToken);
        foreach (var cancelledItem in cancelledItems)
        {
            await _eventPublisher.PublishAsync(new ItemCancelledEvent(cancelledItem), cancellationToken);
        }

        // 5. Return response
        return _mapper.Map<UpdateSaleResult>(updatedSale);
    }
}
